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

        public override bool ActUponException(string path, Exception e, IServiceProvider provider)
        {
            // Let the base class determine if we should act on this exception
            if (!base.ActUponException(path, e, provider))
                return false;

            // Check if we've reached the threshold to open the circuit
            if (circuitBreaker.IsThresholdReached())
            {
                circuitBreaker.MoveToOpenState();
                try
                {
                    circuitBreaker.Monitor(provider)?.Trip(path, e, this, provider);
                }
                catch (Exception ex)
                {
                    provider?.GetService<ILogger>()?.Error(ex);
                }
            }
            return true;
        }

        public override void ProtectedCodeHasBeenCalled()
        {
            // If there was a previous error and enough time has passed since the reset timeout,
            // we can reset the failure count to give the service a fresh start
            if (circuitBreaker.LastErrorTime.HasValue)
            {
                var timeSinceLastError = DateTime.UtcNow - circuitBreaker.LastErrorTime.Value;
                if (timeSinceLastError >= circuitBreaker.ResetTimeout)
                {
                    circuitBreaker.ResetFailureCount();
                }
            }
        }
    }
}