using System;

namespace Stardust.Interstellar.Rest.Client.CircuitBreaker
{
    public class SuspendedDependencyException : Exception
    {
        public SuspendedDependencyException(string message, Exception innerException) : base(message, innerException)
        {
        }

        public SuspendedDependencyException(string message):base(message)
        {
            
        }
    }
}