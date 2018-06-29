using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http.Headers;
using Microsoft.Extensions.Primitives;
using Stardust.Interstellar.Rest.Extensions;

namespace Stardust.Interstellar.Rest.Annotations.UserAgent
{
    public class FixedClientUserAgentAttribute : Attribute, IHeaderHandler, IHeaderInspector
    {
        private readonly string userAgentName;

        public FixedClientUserAgentAttribute(string userAgentName)
        {
            this.userAgentName = userAgentName;
        }

        /// <summary>
        /// The order of execution. Lower numbers will be processed first
        /// </summary>
        public int ProcessingOrder => -1;

        /// <summary>
        /// Set custom header values on sending request to a service
        /// </summary>
        /// <param name="req"></param>
        void IHeaderHandler.SetHeader(HttpWebRequest req)
        {
            req.UserAgent = userAgentName;
        }

        /// <summary>
        /// Get header values form a service response
        /// </summary>
        /// <param name="response"></param>
        void IHeaderHandler.GetHeader(HttpWebResponse response)
        {

        }

        /// <summary>
        /// Get custom header values received from the client 
        /// </summary>
        /// <param name="headers"></param>
        void IHeaderHandler.GetServiceHeader(HttpRequestHeaders headers)
        {

        }

        public void GetServiceHeader(IDictionary<string, StringValues> headers)
        {

        }

        void IHeaderHandler.SetServiceHeaders(HttpResponseHeaders headers)
        {

        }

        public void SetServiceHeaders(IDictionary<string, StringValues> headers)
        {

        }

        IHeaderHandler[] IHeaderInspector.GetHandlers(IServiceProvider locator)
        {
            return new IHeaderHandler[] { new FixedClientUserAgentAttribute(userAgentName) };
        }
    }
}
