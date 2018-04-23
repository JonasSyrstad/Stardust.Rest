using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using Microsoft.Extensions.Primitives;
using Stardust.Interstellar.Rest.Extensions;

namespace Stardust.Interstellar.Rest.Annotations
{
    internal class PerformanceHeadersHandler : StatefullHeaderHandlerBase
    {
        internal const string StardustTimerKey = "x-execution-timer";

        /// <summary>
        /// The order of execution. Lower numbers will be processed first
        /// </summary>
        public override int ProcessingOrder => -2;

        protected override void DoSetHeader(IStateContainer state, HttpWebRequest req)
        {
            if (state.ContainsKey(StardustTimerKey)) return;
            state.SetState(StardustTimerKey, Stopwatch.StartNew());
        }

        protected override void DoGetHeader(IStateContainer state, HttpWebResponse response)
        {
            var sw = state.GetState<Stopwatch>(StardustTimerKey);
            if (sw == null) return;
            sw.Stop();
            var server = response.Headers[StardustTimerKey];
            if (!string.IsNullOrWhiteSpace(server))
            {
                var serverTime = long.Parse(server);
                var latency = sw.ElapsedMilliseconds - serverTime;
                state.Extras.Add("latency", latency);
                state.Extras.Add("serverTime", serverTime);
                state.Extras.Add("totalTime", sw.ElapsedMilliseconds);
            }

        }

        protected override void DoSetServiceHeaders(IStateContainer state, HttpResponseHeaders headers)
        {
            var sw = state.GetState<Stopwatch>(StardustTimerKey);
            if (sw == null) return;
            sw.Stop();
            headers.Add(StardustTimerKey, sw.ElapsedMilliseconds.ToString());
        }

        protected override void DoGetServiceHeader(IStateContainer state, IDictionary<string, StringValues> headers)
        {
            state.Add(StardustTimerKey, Stopwatch.StartNew());
        }

        protected override void DoSetServiceHeaders(IStateContainer state, IDictionary<string, StringValues> headers)
        {
            var sw = state.GetState<Stopwatch>(StardustTimerKey);
            if (sw == null) return;
            sw.Stop();
            headers.Add(StardustTimerKey, sw.ElapsedMilliseconds.ToString());
        }

        protected override void DoGetServiceHeader(IStateContainer state, HttpRequestHeaders headers)
        {
            state.Add(StardustTimerKey, Stopwatch.StartNew());
        }
    }
}