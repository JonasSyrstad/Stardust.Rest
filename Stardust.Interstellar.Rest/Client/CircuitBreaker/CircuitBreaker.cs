using System;
using System.Net;
using System.Threading.Tasks;
using Stardust.Interstellar.Rest.Annotations;
using Stardust.Interstellar.Rest.Common;

namespace Stardust.Interstellar.Rest.Client.CircuitBreaker
{
    public class CircuitBreaker : ICircuitBreaker, ICircuit
    {
        private readonly object syncLock = new object();
        private CircuitBreakerStateBase state;
        private Exception exceptionFromLastAttemptCall;
        private ICircuitBreakerMonitor monitor;

        public CircuitBreaker(int threshold, TimeSpan timeout, TimeSpan resetTimeout, Type[] ignoredExceptions, HttpStatusCode[] ignoredStatusCodes, Type circuitBreakerMonitor, IServiceProvider serviceLocator)
        {
            if (threshold < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(threshold), "Threshold should be greater than 0");
            }

            if (timeout.TotalMilliseconds < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(timeout), "Timeout should be greater than 0");
            }

            if (resetTimeout.TotalMilliseconds < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(resetTimeout), "Reset timeout should be greater than 0");
            }

            Threshold = threshold;
            Timeout = timeout;
            ResetTimeout = resetTimeout;
            IgnoredExceptions = ignoredExceptions ?? new Type[0];
            IgnoredStatusCodes = ignoredStatusCodes ?? new HttpStatusCode[0];
            
            MoveToClosedState();
            
            if (circuitBreakerMonitor != null)
            {
                try
                {
                    monitor = (ICircuitBreakerMonitor)Activator.CreateInstance(circuitBreakerMonitor, serviceLocator);
                }
                catch (Exception ex)
                {
                    serviceLocator?.GetService<ILogger>()?.Message($"Failed to create circuit breaker monitor: {ex.Message}");
                    // Fall back to NullBreaker if monitor creation fails
                }
            }
        }

        public CircuitBreaker(CircuitBreakerAttribute attribute, IServiceProvider serviceLocator) 
            : this(attribute.Threshold, attribute.Timeout, attribute.ResetTimeout, attribute.IgnoredExceptionTypes, attribute.IgnoredStatusCodes, attribute.Monitor, serviceLocator)
        {
        }

        public int Failures { get; private set; }

        DateTime? ICircuit.LastFailure => LastErrorTime;

        TimeSpan? ICircuit.SuspendedTime => LastErrorTime.HasValue ? 
            (TimeSpan?)((LastErrorTime.Value + ResetTimeout) - DateTime.UtcNow) : (TimeSpan?)null;

        Exception ICircuit.LastError => GetExceptionFromLastAttemptCall();

        public int Threshold { get; }

        public TimeSpan Timeout { get; }

        public TimeSpan ResetTimeout { get; }

        public Type[] IgnoredExceptions { get; }

        public bool IsClosed
        {
            get 
            { 
                lock (syncLock)
                {
                    return state.Update() is ClosedState; 
                }
            }
        }

        public bool IsOpen
        {
            get 
            { 
                lock (syncLock)
                {
                    return state.Update() is OpenState; 
                }
            }
        }

        public bool IsHalfOpen
        {
            get 
            { 
                lock (syncLock)
                {
                    return state.Update() is HalfOpenState; 
                }
            }
        }

        public HttpStatusCode[] IgnoredStatusCodes { get; }
        public DateTime? LastErrorTime { get; private set; }
        public string ServiceName { get; set; }

        public ICircuitBreakerMonitor Monitor(IServiceProvider serviceLocator)
        {
            if (monitor == null)
            {
                monitor = serviceLocator?.GetService<ICircuitBreakerMonitor>() ?? new NullBreaker();
            }
            return monitor;
        }

        internal CircuitBreakerStateBase MoveToClosedState()
        {
            state = new ClosedState(this);
            return state;
        }

        internal CircuitBreakerStateBase MoveToOpenState()
        {
            state = new OpenState(this);
            return state;
        }

        internal CircuitBreakerStateBase MoveToHalfOpenState()
        {
            state = new HalfOpenState(this);
            return state;
        }

        public Exception GetExceptionFromLastAttemptCall()
        {
            lock (syncLock)
            {
                return exceptionFromLastAttemptCall;
            }
        }

        public virtual ResultWrapper Execute(string path, Func<ResultWrapper> func, IServiceProvider provider)
        {
            ResultWrapper result;
            if (PreCallProcessing(path, out result)) return result;
            
            try
            {
                result = func();
                if (result?.Error != null)
                {
                    PostProcessing(path, result, provider);
                    CleanupWebException(result.Error);
                    return result;
                }
            }
            catch (Exception e)
            {
                return PostProcessing(path, e, provider);
            }

            PostProcessing();
            return result;
        }

        public virtual async Task<ResultWrapper> ExecuteAsync(string path, Func<Task<ResultWrapper>> func, IServiceProvider provider)
        {
            ResultWrapper result;
            if (PreCallProcessing(path, out result)) return result;
            
            try
            {
                result = await func();
                if (result?.Error != null)
                {
                    PostProcessing(path, result, provider);
                    CleanupWebException(result.Error);
                    return result;
                }
            }
            catch (Exception e)
            {
                return PostProcessing(path, e, provider);
            }

            PostProcessing();
            return result;
        }

        public T Invoke<T>(string actionUrl, Func<T> func, IServiceProvider provider)
        {
            T result;
            if (PreCallProcessingForInvoke(actionUrl))
            {
                // Circuit is open, throw exception
                lock (syncLock)
                {
                    var suspended = LastErrorTime.HasValue ? 
                        (LastErrorTime.Value + ResetTimeout) - DateTime.UtcNow : TimeSpan.Zero;
                    throw new SuspendedDependencyException(
                        $"Calls to {actionUrl} are suspended for {suspended.TotalMinutes:F1} minutes",
                        exceptionFromLastAttemptCall);
                }
            }

            try
            {
                result = func();
            }
            catch (Exception e)
            {
                lock (syncLock)
                {
                    exceptionFromLastAttemptCall = e;
                    state.ActUponException(actionUrl, e, provider);
                }
                throw;
            }

            PostProcessing();
            return result;
        }

        public async Task<T> InvokeAsync<T>(string actionUrl, Func<Task<T>> func, IServiceProvider provider)
        {
            T result;
            if (PreCallProcessingForInvoke(actionUrl))
            {
                // Circuit is open, throw exception
                lock (syncLock)
                {
                    var suspended = LastErrorTime.HasValue ? 
                        (LastErrorTime.Value + ResetTimeout) - DateTime.UtcNow : TimeSpan.Zero;
                    throw new SuspendedDependencyException(
                        $"Calls to {actionUrl} are suspended for {suspended.TotalMinutes:F1} minutes",
                        exceptionFromLastAttemptCall);
                }
            }

            try
            {
                result = await func();
            }
            catch (Exception e)
            {
                lock (syncLock)
                {
                    exceptionFromLastAttemptCall = e;
                    state.ActUponException(actionUrl, e, provider);
                }
                throw;
            }

            PostProcessing();
            return result;
        }

        public void Invoke(string actionUrl, Action func, IServiceProvider provider)
        {
            if (PreCallProcessingForInvoke(actionUrl))
            {
                // Circuit is open, throw exception
                lock (syncLock)
                {
                    var suspended = LastErrorTime.HasValue ? 
                        (LastErrorTime.Value + ResetTimeout) - DateTime.UtcNow : TimeSpan.Zero;
                    throw new SuspendedDependencyException(
                        $"Calls to {actionUrl} are suspended for {suspended.TotalMinutes:F1} minutes",
                        exceptionFromLastAttemptCall);
                }
            }

            try
            {
                func();
            }
            catch (Exception e)
            {
                lock (syncLock)
                {
                    exceptionFromLastAttemptCall = e;
                    state.ActUponException(actionUrl, e, provider);
                }
                throw;
            }

            PostProcessing();
        }

        public async Task InvokeAsync(string actionUrl, Func<Task> func, IServiceProvider provider)
        {
            if (PreCallProcessingForInvoke(actionUrl))
            {
                // Circuit is open, throw exception
                lock (syncLock)
                {
                    var suspended = LastErrorTime.HasValue ? 
                        (LastErrorTime.Value + ResetTimeout) - DateTime.UtcNow : TimeSpan.Zero;
                    throw new SuspendedDependencyException(
                        $"Calls to {actionUrl} are suspended for {suspended.TotalMinutes:F1} minutes",
                        exceptionFromLastAttemptCall);
                }
            }

            try
            {
                await func();
            }
            catch (Exception e)
            {
                lock (syncLock)
                {
                    exceptionFromLastAttemptCall = e;
                    state.ActUponException(actionUrl, e, provider);
                }
                throw;
            }

            PostProcessing();
        }

        private bool PreCallProcessingForInvoke(string actionUrl)
        {
            lock (syncLock)
            {
                state.ProtectedCodeIsAboutToBeCalled();
                exceptionFromLastAttemptCall = null;
                return state is OpenState;
            }
        }

        private bool PreCallProcessing(string path, out ResultWrapper execute)
        {
            lock (syncLock)
            {
                state.ProtectedCodeIsAboutToBeCalled();
                if (state is OpenState)
                {
                    var suspended = LastErrorTime.HasValue ? 
                        (LastErrorTime.Value + ResetTimeout) - DateTime.UtcNow : TimeSpan.Zero;
                    
                    execute = new ResultWrapper
                    {
                        Error = new SuspendedDependencyException(
                            $"Calls to {path} are suspended for {suspended.TotalMinutes:F1} minutes", 
                            exceptionFromLastAttemptCall),
                        Status = HttpStatusCode.ServiceUnavailable,
                        StatusMessage = $"Service temporarily unavailable - suspended for {suspended.TotalMinutes:F1} minutes",
                        ActionId = Guid.NewGuid().ToString()
                    };
                    
                    var state = execute.GetState();
                    state.Extras.Add("x-serviceSuspended", $"{suspended.TotalMinutes:F1} minutes");
                    state.Extras.Add("x-suspendedService", path);
                    return true;
                }
            }
            
            exceptionFromLastAttemptCall = null;
            execute = null;
            return false;
        }

        private ResultWrapper PostProcessing(string path, Exception e, IServiceProvider provider)
        {
            lock (syncLock)
            {
                exceptionFromLastAttemptCall = e;
                state.ActUponException(path, e, provider);
            }
            return new ResultWrapper { Error = e, ActionId = Guid.NewGuid().ToString() };
        }

        private void PostProcessing(string path, ResultWrapper result, IServiceProvider provider)
        {
            lock (syncLock)
            {
                exceptionFromLastAttemptCall = result.Error;
                state.ActUponException(path, result.Error, provider);
            }
        }

        private void PostProcessing()
        {
            lock (syncLock)
            {
                state.ProtectedCodeHasBeenCalled();
            }
        }

        private static void CleanupWebException(Exception exception)
        {
            var webException = exception as WebException;
            if (webException != null)
            {
                try
                {
                    webException.Response?.Close();
                    webException.Response?.Dispose();
                }
                catch
                {
                    // Ignore cleanup errors
                }
            }
        }

        public void IncreaseFailureCount()
        {
            Failures++;
            LastErrorTime = DateTime.UtcNow;
        }

        public void ResetFailureCount()
        {
            Failures = 0;
            LastErrorTime = null;
        }

        public bool IsThresholdReached()
        {
            return Failures >= Threshold;
        }
    }
}