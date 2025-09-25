using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Stardust.Interstellar.Rest.Annotations;
using Stardust.Interstellar.Rest.Common;

namespace Stardust.Interstellar.Rest.Client.CircuitBreaker
{
    internal class NullBreaker : ICircuitBreaker, ICircuitBreakerMonitor
    {
        public ResultWrapper Execute(string path, Func<ResultWrapper> func, IServiceProvider provider)
        {
            var result = func();

            CleanupWebException(result?.Error);
            return result;
        }

        public async Task<ResultWrapper> ExecuteAsync(string path, Func<Task<ResultWrapper>> func, IServiceProvider provider)
        {
            var result = await func();

            CleanupWebException(result?.Error);
            return result;
        }

        public T Invoke<T>(string actionUrl, Func<T> func, IServiceProvider provider)
        {
            return func();
        }

        public async Task<T> InvokeAsync<T>(string actionUrl, Func<Task<T>> func, IServiceProvider provider)
        {
            return await func();
        }

        public void Invoke(string actionUrl, Action func, IServiceProvider provider)
        {
            func();
        }

        public async Task InvokeAsync(string actionUrl, Func<Task> func, IServiceProvider provider)
        {
            await func();
        }

        public void Trip(string circuitBreakerServiceName, Exception exception, ICircuitBreakerState state, IServiceProvider provider)
        {
            var logger = provider?.GetService<ILogger>();
            logger?.Error(exception);
            
            var webEx = exception as WebException ?? (exception as AggregateException)?.InnerException as WebException;
            if (webEx?.Response != null)
            {
                logger?.Message($"Circuit breaker tripped for service {circuitBreakerServiceName}. Action url: {webEx.Response.ResponseUri}. State: {state?.State}");
            }
            else
            {
                logger?.Message($"Circuit breaker tripped for service {circuitBreakerServiceName}. Exception: {exception?.GetType().Name}. State: {state?.State}");
            }
        }

        public bool IsExceptionIgnorable(Exception exception)
        {
            // The NullBreaker doesn't ignore any exceptions by default
            // Let the actual circuit breaker configuration handle the ignore logic
            return false;
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
    }
}