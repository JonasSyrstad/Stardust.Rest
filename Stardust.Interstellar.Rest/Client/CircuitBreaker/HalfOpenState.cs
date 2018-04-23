using System;
using Stardust.Interstellar.Rest.Common;

namespace Stardust.Interstellar.Rest.Client.CircuitBreaker
{
    internal class HalfOpenState : CircuitBreakerStateBase
    {
        public HalfOpenState(CircuitBreaker circuitBreaker) : base(circuitBreaker) { }

        public override bool ActUponException(string path, Exception e)
        {
            if(base.ActUponException(path,e))
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
            base.ProtectedCodeHasBeenCalled();
            circuitBreaker.MoveToClosedState();
            
        }
    }
}