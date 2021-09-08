using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Primitives;

namespace Stardust.Interstellar.Rest.Extensions
{
    public abstract class StatefullHeaderHandlerBase : IHeaderHandler
    {
        /// <summary>
        /// The order of execution. Lower numbers will be processed first
        /// </summary>
        public abstract int ProcessingOrder { get; }

        /// <summary>
        /// Set custom header values on sending request to a service
        /// </summary>
        /// <param name="req"></param>
        public void SetHeader(HttpRequestMessage req)
        {
            var state = req.GetState();
            DoSetHeader(state, req);
        }


        protected abstract void DoSetHeader(IStateContainer state, HttpRequestMessage req);

        /// <summary>
        /// Get header values form a service response
        /// </summary>
        /// <param name="response"></param>
        public void GetHeader(HttpResponseMessage response)
        {
            try
            {
                var state = response.Headers.GetState();
                DoGetHeader(state, response);
            }
            catch (Exception)
            {
                // ignored
            }
        }



        protected abstract void DoGetHeader(IStateContainer state, HttpResponseMessage response);


        protected abstract void DoSetServiceHeaders(IStateContainer state, HttpResponseHeaders headers);

        /// <summary>
        /// Get custom header values received from the client 
        /// </summary>
        /// <param name="headers"></param>
        public void GetServiceHeader(HttpRequestHeaders headers)
        {
            var state = headers.GetState();
            DoGetServiceHeader(state, headers);
        }

        public void GetServiceHeader(IDictionary<string, StringValues> headers)
        {
            var state = headers.GetState();
            DoGetServiceHeader(state, headers);
        }

        protected abstract void DoGetServiceHeader(IStateContainer state, IDictionary<string, StringValues> headers);

        public void SetServiceHeaders(HttpResponseHeaders headers)
        {
            var state = headers.GetState();
            DoSetServiceHeaders(state, headers);
        }

        public void SetServiceHeaders(IDictionary<string, StringValues> headers)
        {
            var state = headers.GetState();
            DoSetServiceHeaders(state, headers);
        }

        //Implement in .netcore
        protected abstract void DoSetServiceHeaders(IStateContainer state, IDictionary<string, StringValues> headers);

        //Implement in .net framework
        protected abstract void DoGetServiceHeader(IStateContainer state, HttpRequestHeaders headers);
    }
}
