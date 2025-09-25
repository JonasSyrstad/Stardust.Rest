using System;
using Stardust.Interstellar.Rest.Annotations;
using Stardust.Interstellar.Rest.Common;

namespace Stardust.Interstellar.Rest.Client.CircuitBreaker
{
    internal class HalfOpenState : CircuitBreakerStateBase
    {
        public HalfOpenState(CircuitBreaker circuitBreaker) : base(circuitBreaker) 
        { 
            // Reset failure count when entering half-open state to give the service a chance
            circuitBreaker.ResetFailureCount();
        }

        public override bool ActUponException(string path, Exception e, IServiceProvider provider)
        {
            // Let the base class determine if we should act on this exception
            if (!base.ActUponException(path, e, provider))
                return false;

            // In half-open state, any significant failure immediately opens the circuit again
            circuitBreaker.MoveToOpenState();
            try
            {
                circuitBreaker.Monitor(provider)?.Trip(path, e, this, provider);
            }
            catch (Exception ex)
            {
                provider?.GetService<ILogger>()?.Error(ex);
            }
            
            return true;
        }

        public override void ProtectedCodeHasBeenCalled()
        {
            base.ProtectedCodeHasBeenCalled();
            
            // If we successfully completed a call without exceptions, close the circuit
            circuitBreaker.MoveToClosedState();
        }
    }
}