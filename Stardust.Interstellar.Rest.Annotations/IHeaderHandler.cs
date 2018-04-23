using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Http.Headers;
using Microsoft.Extensions.Primitives;

namespace Stardust.Interstellar.Rest.Extensions
{
    public interface IHeaderHandler
    {
        /// <summary>
        /// The order of execution. Lower numbers will be processed first
        /// </summary>
        int ProcessingOrder { get; }
        /// <summary>
        /// Set custom header values on sending request to a service
        /// </summary>
        /// <param name="req"></param>
        void SetHeader(HttpWebRequest req);

        /// <summary>
        /// Get header values form a service response
        /// </summary>
        /// <param name="response"></param>
        void GetHeader(HttpWebResponse response);



        /// <summary>
        /// Get custom header values received from the client 
        /// </summary>
        /// <param name="headers"></param>
        void GetServiceHeader(HttpRequestHeaders headers);

        void GetServiceHeader(IDictionary<string, StringValues> headers);

        void SetServiceHeaders(HttpResponseHeaders headers);

        void SetServiceHeaders(IDictionary<string, StringValues> headers);
    }
}