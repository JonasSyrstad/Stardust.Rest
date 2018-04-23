using System;
using System.Net;
using System.Net.Http;
using Stardust.Interstellar.Rest.Extensions;

namespace Stardust.Interstellar.Rest.Service
{
    public class AggregateHandler : IErrorHandler
    {
        private readonly IErrorHandler errorHandler;

        private readonly IErrorHandler errorInterceptor;

        public AggregateHandler(IErrorHandler errorHandler, IErrorHandler errorInterceptor)
        {
            this.errorHandler = errorHandler;
            this.errorInterceptor = errorInterceptor;
        }

        /// <summary>
        /// Provide additional excption type to http status code mappings mappings 
        /// </summary>
        /// <param name="exception"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        public HttpResponseMessage ConvertToErrorResponse(Exception exception, HttpRequestMessage request)
        {
            var res1 = errorHandler.ConvertToErrorResponse(exception, request);
            var res2 = errorInterceptor.ConvertToErrorResponse(exception, request);
            if (res1.StatusCode == HttpStatusCode.InternalServerError) return res2;
            return res1;
        }

        /// <summary>
        /// Set to true if this handles all exception types 
        /// </summary>
        public bool OverrideDefaults { get; }

        public Exception ProduceClientException(string statusMessage, HttpStatusCode status, Exception error, string value)
        {
            var ex = errorHandler.ProduceClientException(statusMessage, status, error, value);
            var ex2 = errorInterceptor.ProduceClientException(statusMessage, status, ex, value);
            if (ex != null && ex2 == null) throw new AggregateException(ex);
            if (ex == null && ex2 != null) throw new AggregateException(ex2);
            return new AggregateException(ex, ex2);
        }
    }
}