using System;
using System.Net;
using System.Runtime.Serialization;

namespace Stardust.Interstellar.Rest.Extensions
{
    public class OperationAbortedException : Exception
    {
        public HttpStatusCode StatusCode { get; set; }

        public OperationAbortedException():this(HttpStatusCode.InternalServerError, "Operation aborted")
        {
        }

        public OperationAbortedException(string message) : this(HttpStatusCode.InternalServerError,message)
        {
        }

        public OperationAbortedException(HttpStatusCode statusCode, string message, Exception innerException) : base(message, innerException)
        {
            StatusCode = statusCode;
        }

        public OperationAbortedException(HttpStatusCode statusCode, string message):base(message)
        {
            StatusCode = statusCode;
        }

        protected OperationAbortedException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

        public OperationAbortedException(string message, Exception innerException) : this(HttpStatusCode.InternalServerError,message, innerException)
        {
        }
    }
}