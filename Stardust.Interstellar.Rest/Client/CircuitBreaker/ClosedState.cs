using System;
using Stardust.Interstellar.Rest.Annotations;
using Stardust.Interstellar.Rest.Common;

namespace Stardust.Interstellar.Rest.Client.CircuitBreaker
{
    internal class ClosedState : CircuitBreakerStateBase
    {
        public ClosedState(CircuitBreaker circuitBreaker)
            : base(circuitBreaker)
        {
            circuitBreaker.ResetFailureCount();
        }

        public override bool ActUponException(string path, Exception e)
        {
            base.ActUponException(path, e);
            if (circuitBreaker.IsThresholdReached())
            {
                circuitBreaker.MoveToOpenState();
                try
                {
                    circuitBreaker.Monitor?.Trip(path, e, this);
                }
                catch (Exception ex)
                {
                    base.circuitBreaker._serviceLocator.GetService<ILogger>()?.Error(ex);
                }
            }
            return true;
        }

        public override void ProtectedCodeHasBeenCalled()
        {
            if (!circuitBreaker.LastErrorTime.HasValue) return;
            if (DateTime.UtcNow >= circuitBreaker.LastErrorTime + circuitBreaker.ResetTimeout)
            {
                circuitBreaker.ResetFailureCount();
            }
        }
    }
}