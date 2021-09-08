using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using Microsoft.Extensions.Primitives;
using Stardust.Interstellar.Rest.Extensions;

namespace Stardust.Continuum.Client
{
    public class CallingMachineNameHandler : IHeaderHandler
    {
        private static long receivedTotal = 0;
        private static long receivedLastHour = 0;
        private static DateTime resetTime;
        private static string Version { get; } = $"{typeof(CallingMachineNameHandler).Assembly.GetName().Version.Major}.{typeof(CallingMachineNameHandler).Assembly.GetName().Version.Minor}";
        /// <summary>
        /// The order of execution. Lower numbers will be processed first
        /// </summary>
        public int ProcessingOrder => -1;

        public static long ReceivedBytesTotal => receivedTotal;

        public static long ReceivedLastHour => receivedLastHour;

        public void SetHeader(HttpRequestMessage req)
        {
            req.Headers.Add("x-callingMachine", Environment.MachineName);
            //req.UserAgent += $"(continuum/{Version})";
        }

        public void GetHeader(HttpResponseMessage response)
        {

        }

        public void GetServiceHeader(HttpRequestHeaders headers)
        {
        }

        public void GetServiceHeader(IDictionary<string, StringValues> headers)
        {
        }

        public void SetServiceHeaders(HttpResponseHeaders headers)
        {
            headers.Add("x-service-runtime", "continuum.V.1.2.rc1");
        }

        public void SetServiceHeaders(IDictionary<string, StringValues> headers)
        {
            headers.Add("x-service-runtime", "continuum.V.1.2.rc1");
        }
    }
}