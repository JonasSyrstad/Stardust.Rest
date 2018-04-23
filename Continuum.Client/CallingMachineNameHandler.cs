using System;
using System.Linq;
using System.Net;
using System.Net.Http.Headers;
using Stardust.Interstellar.Rest.Extensions;

namespace Stardust.Continuum.Client
{
    public class CallingMachineNameHandler : IHeaderHandler
    {
        private static long receivedTotal = 0;
        private static long receivedLastHour = 0;
        private static DateTime resetTime;

        /// <summary>
        /// The order of execution. Lower numbers will be processed first
        /// </summary>
        public int ProcessingOrder => -1;

        public static long ReceivedBytesTotal => receivedTotal;

        public static long ReceivedLastHour => receivedLastHour;

        public void SetHeader(HttpWebRequest req)
        {
            req.Headers.Add("x-callingMachine", Environment.MachineName);
        }

        public void GetHeader(HttpWebResponse response)
        {

        }

        public void GetServiceHeader(HttpRequestHeaders headers)
        {
        }

        public void SetServiceHeaders(HttpResponseHeaders headers)
        {
            headers.Add("x-service-runtime", "continuum.V.1.2.rc1");
        }
    }
}