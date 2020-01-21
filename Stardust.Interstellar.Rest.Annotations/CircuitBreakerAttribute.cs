using System;
using System.Net;

namespace Stardust.Interstellar.Rest.Annotations
{
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
}