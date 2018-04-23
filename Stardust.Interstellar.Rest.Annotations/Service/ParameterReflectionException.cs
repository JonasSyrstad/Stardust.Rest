using System;
using System.Runtime.Serialization;

namespace Stardust.Interstellar.Rest.Service
{
    public class ParameterReflectionException : Exception
    {

        public ParameterReflectionException(string message) : base(message)
        {
        }

        public ParameterReflectionException(string message, Exception innerException) : base(message, innerException)
        {

        }

        protected ParameterReflectionException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}