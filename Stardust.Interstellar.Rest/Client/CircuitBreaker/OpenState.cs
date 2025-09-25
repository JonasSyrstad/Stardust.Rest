using System;

namespace Stardust.Interstellar.Rest.Client.CircuitBreaker
{
    internal class OpenState : CircuitBreakerStateBase
    {
        private readonly DateTime openDateTime;
        
        public OpenState(CircuitBreaker circuitBreaker)
            : base(circuitBreaker)
        {
            openDateTime = DateTime.UtcNow;
        }

        public override CircuitBreaker ProtectedCodeIsAboutToBeCalled()
        {
            base.ProtectedCodeIsAboutToBeCalled();
            Update();
            return circuitBreaker;
        }

        public override CircuitBreakerStateBase Update()
        {
            base.Update();
            
            // After the timeout period has elapsed, move to HalfOpen state to test if the service has recovered
            if (DateTime.UtcNow >= openDateTime + circuitBreaker.Timeout)
            {
                return circuitBreaker.MoveToHalfOpenState();
            }
            
            return this;
        }
    }
}