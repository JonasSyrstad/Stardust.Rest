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
            return this.circuitBreaker;
        }
        public virtual void ProtectedCodeHasBeenCalled() { }

        public virtual bool ActUponException(string path, Exception e)
        {
            if (circuitBreaker.Monitor.IsExceptionIgnorable(e)) return false;
            var webEx = e as WebException;
            if (e is AggregateException)
            {
                webEx = e.InnerException as WebException;
            }
            var resp = webEx?.Response as HttpWebResponse;
            if (resp != null && circuitBreaker.IgnoredStatusCodes.Contains(resp.StatusCode)) return false;
            if (circuitBreaker.IgnoredExceptions.Contains(e.GetType())) return false;
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