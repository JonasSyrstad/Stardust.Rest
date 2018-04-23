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
            if (DateTime.UtcNow >= openDateTime + circuitBreaker.Timeout)
            {
                return circuitBreaker.MoveToHalfOpenState();
            }
            return this;
        }
    }
}