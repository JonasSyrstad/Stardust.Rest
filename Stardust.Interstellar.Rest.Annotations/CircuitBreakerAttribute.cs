using System;
using System.Net;

namespace Stardust.Interstellar.Rest.Annotations
{
    /// <summary>
    /// Defines the scope for circuit breaker instances
    /// </summary>
    public enum CircuitBreakerScope
    {
        /// <summary>
        /// Common scope - all calls share the same circuit breaker instance (default)
        /// </summary>
        Common = 0,
        /// <summary>
        /// User scope - each user has their own circuit breaker instance
        /// </summary>
        User = 1,
        /// <summary>
        /// Client scope - each client has their own circuit breaker instance
        /// </summary>
        Client = 2,
        /// <summary>
        /// User and Client scope - each user+client combination has their own circuit breaker instance
        /// </summary>
        UserAndClient = 3
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
            Scope = CircuitBreakerScope.Common; // Default scope
        }

        public CircuitBreakerAttribute(int threshold, int timeoutInMinutes, int resetTimeout)
        {
            Threshold = threshold;
            Timeout = TimeSpan.FromMinutes(timeoutInMinutes);
            ResetTimeout = TimeSpan.FromMinutes(resetTimeout);
            IgnoredExceptionTypes = new[] { typeof(UnauthorizedAccessException), typeof(NullReferenceException) };
            IgnoredStatusCodes = DefaultIgnoredHttpStatusCodes;
            Scope = CircuitBreakerScope.Common; // Default scope
        }

        /// <summary>
        /// Constructor with scope configuration
        /// </summary>
        public CircuitBreakerAttribute(int threshold, int timeoutInMinutes, int resetTimeout, CircuitBreakerScope scope) 
            : this(threshold, timeoutInMinutes, resetTimeout)
        {
            Scope = scope;
        }

        /// <summary>
        /// Constructor with scope and exception/status code configuration
        /// </summary>
        public CircuitBreakerAttribute(int threshold, int timeoutInMinutes, int resetTimeout, CircuitBreakerScope scope, Type[] ignoredExceptionTypes, HttpStatusCode[] ignoredStatusCodes)
            : this(threshold, timeoutInMinutes, resetTimeout, ignoredExceptionTypes, ignoredStatusCodes)
        {
            Scope = scope;
        }

        /// <summary>
        /// Constructor with scope and user/client identifier properties
        /// </summary>
        public CircuitBreakerAttribute(int threshold, int timeoutInMinutes, int resetTimeout, CircuitBreakerScope scope, string userIdentifierProperty, string clientIdentifierProperty)
            : this(threshold, timeoutInMinutes, resetTimeout, scope)
        {
            UserIdentifierProperty = userIdentifierProperty;
            ClientIdentifierProperty = clientIdentifierProperty;
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

        /// <summary>
        /// Defines the scope for circuit breaker instances. Default is Common.
        /// </summary>
        public CircuitBreakerScope Scope { get; set; } = CircuitBreakerScope.Common;

        /// <summary>
        /// Property name or claim type to use for user identification when Scope includes User.
        /// If not specified, defaults to standard identity claims like "sub", "name", or "user_id".
        /// </summary>
        public string UserIdentifierProperty { get; set; }

        /// <summary>
        /// Property name or claim type to use for client identification when Scope includes Client.
        /// If not specified, defaults to standard client claims like "client_id", "azp", or "aud".
        /// </summary>
        public string ClientIdentifierProperty { get; set; }

        /// <summary>
        /// When true, uses case-sensitive comparison for user/client identifiers. Default is false.
        /// </summary>
        public bool CaseSensitiveIdentifiers { get; set; } = false;

        /// <summary>
        /// Maximum number of circuit breaker instances to cache per service interface.
        /// Prevents memory leaks in high-volume multi-tenant scenarios. Default is 1000.
        /// Oldest instances are evicted when limit is reached.
        /// </summary>
        public int MaxInstancesPerService { get; set; } = 1000;

        public Type Monitor
        {
            get { return _monitor; }
            set
            {
                if (value != null && !typeof(ICircuitBreakerMonitor).IsAssignableFrom(value))
                    throw new InvalidCastException($"Unable to assign {value.FullName} to {typeof(ICircuitBreakerMonitor).FullName}");
                _monitor = value;
            }
        }
    }
}