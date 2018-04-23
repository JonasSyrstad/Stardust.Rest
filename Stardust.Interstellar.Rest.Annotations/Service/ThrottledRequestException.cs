using System;

namespace Stardust.Interstellar.Rest.Service
{
    public class ThrottledRequestException : Exception
    {
        public long WaitValue { get; }

        public ThrottledRequestException(long waitValue)
        {
            WaitValue = waitValue;
        }

        public ThrottledRequestException()
        {
            throw new NotImplementedException();
        }

        public ThrottledRequestException(Exception innerException) : base("Throttled request", innerException)
        {
        }
    }
}