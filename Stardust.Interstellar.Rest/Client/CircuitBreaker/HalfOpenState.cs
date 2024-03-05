using System;
using Stardust.Interstellar.Rest.Annotations;
using Stardust.Interstellar.Rest.Common;

namespace Stardust.Interstellar.Rest.Client.CircuitBreaker
{
    internal class HalfOpenState : CircuitBreakerStateBase
    {
        public HalfOpenState(CircuitBreaker circuitBreaker) : base(circuitBreaker) { }

        public override bool ActUponException(string path, Exception e, IServiceProvider provider)
        {
            if(base.ActUponException(path,e,provider))
            {
                
                circuitBreaker.MoveToOpenState();
                try
                {
                    circuitBreaker.Monitor(provider)?.Trip(path, e, this,provider);
                }
                catch (Exception ex)
                {
                    provider.GetService<ILogger>()?.Error(ex);
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