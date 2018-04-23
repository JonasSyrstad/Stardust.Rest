using System;
using System.Net;
using System.Runtime.Serialization;

namespace Stardust.Interstellar.Rest.Client
{
    [Serializable]
    public class RestWrapperException : Exception
    {
        public RestWrapperException()
        {
        }

        public RestWrapperException(string message)
            : base(message)
        {
        }

        public RestWrapperException(string message, Exception innerException) :
            base(message, innerException)
        {
        }

        protected RestWrapperException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        public RestWrapperException(string message, HttpStatusCode httpStatus, object response, Exception innerException)
            : base(message, innerException)
        {
            this.HttpStatus = httpStatus;
            this.Response = response;
        }

        public RestWrapperException(string message, HttpStatusCode status, Exception innerException)
            : this(message, status, null, innerException)
        {
            this.HttpStatus = status;
        }

        public HttpStatusCode HttpStatus { get; }

        public object Response { get; }
    }

    [Serializable]
    internal class RestWrapperException<T> : RestWrapperException
    {
        public RestWrapperException()
        {
        }

        public RestWrapperException(string message) : base(message)
        {
        }

        public RestWrapperException(string message, Exception innerException) : base(message, innerException)
        {
        }

        public RestWrapperException(string message, HttpStatusCode httpStatus, object response, Exception error) : base(message, httpStatus, response, error)
        {
        }

        protected RestWrapperException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

        public new T Response => (T)base.Response;
    }
}