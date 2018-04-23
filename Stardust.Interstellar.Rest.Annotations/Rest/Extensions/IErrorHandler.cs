using System;
using System.Net;
using System.Net.Http;

namespace Stardust.Interstellar.Rest.Extensions
{
    public interface IErrorHandler
    {
        /// <summary>
        /// Provide additional excption type to http status code mappings mappings 
        /// </summary>
        /// <param name="exception"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        HttpResponseMessage ConvertToErrorResponse(Exception exception, HttpRequestMessage request);

        /// <summary>
        /// Set to true if this handles all exception types 
        /// </summary>
        bool OverrideDefaults { get;  }

        Exception ProduceClientException(string statusMessage, HttpStatusCode status, Exception error, string value);
    }
}