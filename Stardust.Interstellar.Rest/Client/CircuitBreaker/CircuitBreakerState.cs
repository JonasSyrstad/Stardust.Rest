using System;
using System.Linq;
using System.Net;
using Stardust.Interstellar.Rest.Annotations;

namespace Stardust.Interstellar.Rest.Client.CircuitBreaker
{
    internal abstract class CircuitBreakerStateBase : ICircuitBreakerState
    {
        protected readonly CircuitBreaker circuitBreaker;

        protected CircuitBreakerStateBase(CircuitBreaker circuitBreaker)
        {
            this.circuitBreaker = circuitBreaker;
        }

        public virtual CircuitBreaker ProtectedCodeIsAboutToBeCalled()
        {
            return circuitBreaker;
        }
        public virtual void ProtectedCodeHasBeenCalled() { }

        public virtual bool ActUponException(string path, Exception e, IServiceProvider provider)
        {
            // Check if the monitor considers this exception ignorable
            if (circuitBreaker.Monitor(provider).IsExceptionIgnorable(e)) 
                return false;

            // Check if this exception type is in the ignored list
            if (circuitBreaker.IgnoredExceptions?.Contains(e.GetType()) == true) 
                return false;

            // Handle WebException and check status codes
            var webEx = e as WebException;
            if (e is AggregateException aggregateEx)
            {
                webEx = aggregateEx.InnerException as WebException;
            }

            if (webEx?.Response is HttpWebResponse httpResp)
            {
                // Check if this status code should be ignored
                if (circuitBreaker.IgnoredStatusCodes?.Contains(httpResp.StatusCode) == true)
                    return false;

                // Ignore client errors (4xx) and certain specific server errors that indicate application logic issues
                // rather than service unavailability, unless they're specifically configured to be monitored
                var statusCode = (int)httpResp.StatusCode;
                if (statusCode >= 400 && statusCode < 500)
                {
                    // Most 4xx errors should not trip the circuit breaker as they indicate client errors
                    // However, 429 (Too Many Requests) and 418 (I'm a teapot, often used for rate limiting)
                    // could indicate service overload and should trip the breaker
                    if (statusCode != 429 && statusCode != 418)
                        return false;
                }
            }

            // If we get here, this is an exception that should trip the circuit breaker
            circuitBreaker.IncreaseFailureCount();
            return true;
        }

        public virtual CircuitBreakerStateBase Update()
        {
            return this;
        }

        public ICircuit Circuit => circuitBreaker;
        public string State => GetType().Name;
    }
}