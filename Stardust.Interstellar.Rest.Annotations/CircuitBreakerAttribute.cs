using System;
using System.Collections.Concurrent;
using System.Net;

namespace Stardust.Interstellar.Rest.Annotations
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Interface | AttributeTargets.Assembly)]
    public class ThrottlingAttribute : Attribute
    {
        private readonly Type _throttlingManager;
        private readonly long? _maxRequestsPerSecound;

        public ThrottlingAttribute(long maxRequestsPerSecound)
        {
            _maxRequestsPerSecound = maxRequestsPerSecound;
        }

        public ThrottlingAttribute(Type throttlingManager)
        {
            if (!typeof(IThrottlingManager).IsAssignableFrom(throttlingManager)) throw new InvalidCastException($"Unable to cast {throttlingManager.FullName} to {nameof(IThrottlingManager)}");
            _throttlingManager = throttlingManager;
        }

        public IThrottlingManager GetManager(AppliesToTypes appliesTo)
        {
            if (_maxRequestsPerSecound != null)
                return new RequestsPerSecoundManager(_maxRequestsPerSecound, appliesTo);
            return (IThrottlingManager)Activator.CreateInstance(_throttlingManager);
        }
    }

    public class RequestsPerSecoundManager : IThrottlingManager
    {
        private readonly long? _maxRequestsPerSecound;
        private readonly AppliesToTypes _appliesTo;

        public RequestsPerSecoundManager(long? maxRequestsPerSecound, AppliesToTypes appliesTo, long waitTime = 1000)
        {
            _maxRequestsPerSecound = maxRequestsPerSecound;
            _appliesTo = appliesTo;
            _waitTime = waitTime;
        }

        private static ConcurrentDictionary<string, CounterItem> reqPerSecCounter = new ConcurrentDictionary<string, CounterItem>();
        private readonly long _waitTime;

        public long? IsThrottled(string method, string service, string host)
        {
            CounterItem validator;
            switch (_appliesTo)
            {
                case AppliesToTypes.Method:

                    if (!reqPerSecCounter.TryGetValue($"method:{method}", out validator))
                    {
                        validator = new CounterItem(_maxRequestsPerSecound.Value, _waitTime);
                        reqPerSecCounter.TryAdd($"method:{method}", validator);
                    }
                    break;
                case AppliesToTypes.Service:
                    if (!reqPerSecCounter.TryGetValue($"service:{service}", out validator))
                    {
                        validator = new CounterItem(_maxRequestsPerSecound.Value, _waitTime);
                        reqPerSecCounter.TryAdd($"service:{service}", validator);
                    }
                    break;
                case AppliesToTypes.Host:
                    if (!reqPerSecCounter.TryGetValue($"host:{host}", out validator))
                    {
                        validator = new CounterItem(_maxRequestsPerSecound.Value, _waitTime);
                        reqPerSecCounter.TryAdd($"host:{host}", validator);
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            return validator.CountAndValidate();
        }

    }

    internal class CounterItem
    {
        private DateTime CounterSecound = NowTruncated;
        private long _counter = 0;
        private readonly long _maxLimit;
        private readonly long _waitTime = 1000;

        public CounterItem(long maxLimit)
        {
            _maxLimit = maxLimit;
        }

        public CounterItem(long maxLimit, long waitTime)
        {
            _maxLimit = maxLimit;
            _waitTime = waitTime;
        }

        private static DateTime Truncate(DateTime dateTime, TimeSpan timeSpan)
        {
            if (timeSpan == TimeSpan.Zero) return dateTime;
            return dateTime.AddTicks(-(dateTime.Ticks % timeSpan.Ticks));
        }

        public long? CountAndValidate()
        {
            if (CounterSecound == NowTruncated)
            {
                _counter++;
                return _counter < _maxLimit ? (long?)null : _waitTime;
            }
            else
            {
                _counter = 0;
                CounterSecound = NowTruncated;
                _counter++;
                return _counter < _maxLimit ? (long?)null : _waitTime;
            }
        }

        private static DateTime NowTruncated => Truncate(DateTime.UtcNow, TimeSpan.FromMilliseconds(1));
    }

    public enum AppliesToTypes
    {
        Method,
        Service,
        Host
    }

    public interface IThrottlingManager
    {
        /// <summary>
        /// Determines whether the specified method is throttled. If throttled, it returns the wait time in ms, if not null
        /// </summary>
        /// <param name="method">The method.</param>
        /// <param name="service">The service.</param>
        /// <param name="host">The host.</param>
        /// <returns></returns>
        long? IsThrottled(string method, string service, string host);
    }

    /// <summary>
    /// Apply the Circuit breaker pattern to the service client
    /// </summary>
    [AttributeUsage(AttributeTargets.Interface)]
    public class CircuitBreakerAttribute : Attribute
    {
        private Type _monitor;

        public CircuitBreakerAttribute(int threshold, int timeoutInMinutes, Type[] ignoredExceptionTypes, HttpStatusCode[] ignoredStatusCodes) : this(threshold, timeoutInMinutes, timeoutInMinutes, ignoredExceptionTypes, ignoredStatusCodes)
        { }


        public CircuitBreakerAttribute(int threshold, int timeoutInMinutes) : this(threshold, timeoutInMinutes, timeoutInMinutes)
        { }

        public CircuitBreakerAttribute(int threshold, int timeoutInMinutes, int resetTimeout, Type[] ignoredExceptionTypes, HttpStatusCode[] ignoredStatusCodes)
        {
            Threshold = threshold;
            Timeout = TimeSpan.FromMinutes(timeoutInMinutes);
            IgnoredExceptionTypes = ignoredExceptionTypes;
            IgnoredStatusCodes = ignoredStatusCodes;
            ResetTimeout = TimeSpan.FromMinutes(resetTimeout);
        }


        public CircuitBreakerAttribute(int threshold, int timeoutInMinutes, int resetTimeout)
        {
            Threshold = threshold;
            Timeout = TimeSpan.FromMinutes(timeoutInMinutes);
            ResetTimeout = TimeSpan.FromMinutes(resetTimeout);
            IgnoredExceptionTypes = new[] { typeof(UnauthorizedAccessException), typeof(NullReferenceException) };
            IgnoredStatusCodes = DefaultIgnoredHttpStatusCodes;
        }

        public static HttpStatusCode[] DefaultIgnoredHttpStatusCodes => new[]
        {
            HttpStatusCode.Forbidden,
            HttpStatusCode.Unauthorized,
            HttpStatusCode.PreconditionFailed,
            HttpStatusCode.Ambiguous,
            HttpStatusCode.BadRequest,
            HttpStatusCode.Conflict,
            HttpStatusCode.NotFound,
            HttpStatusCode.Gone,
            HttpStatusCode.RequestEntityTooLarge,
            HttpStatusCode.RequestedRangeNotSatisfiable,
            HttpStatusCode.NotImplemented,
            HttpStatusCode.UnsupportedMediaType,
            HttpStatusCode.UseProxy,

        };

        public int Threshold { get; set; }
        public TimeSpan Timeout { get; set; }
        public Type[] IgnoredExceptionTypes { get; set; }
        public HttpStatusCode[] IgnoredStatusCodes { get; set; }
        public TimeSpan ResetTimeout { get; set; }

        public Type Monitor
        {
            get { return _monitor; }
            set
            {
                if (!typeof(ICircuitBreakerMonitor).IsAssignableFrom(value))
                    throw new InvalidCastException($"Unable to assign {value.FullName} to {typeof(ICircuitBreakerMonitor).FullName}");
                _monitor = value;
            }
        }
    }

    public interface ICircuitBreakerMonitor
    {
        /// <summary>
        /// The Circuit Breaker is opened. Add notifications and proactive efforts here
        /// </summary>
        /// <param name="circuitBreakerServiceName"></param>
        /// <param name="exception"></param>
        /// <param name="state"></param>
        void Trip(string circuitBreakerServiceName, Exception exception, ICircuitBreakerState state, IServiceProvider provider);

        /// <summary>
        /// Programatically determine if an exception is a part of the application flow of control or if it is broken and should be suspended
        /// </summary>
        /// <param name="exception"></param>
        /// <returns></returns>
        bool IsExceptionIgnorable(Exception exception);
    }

    public interface ICircuitBreakerState
    {
        ICircuit Circuit { get; }

        string State { get; }
    }

    public interface ICircuit
    {
        int Failures { get; }

        DateTime? LastFailure { get; }

        TimeSpan? SuspendedTime { get; }

        Exception LastError { get; }
    }
}