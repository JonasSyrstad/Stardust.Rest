using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
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

        protected override void DoSetHeader(IStateContainer state, HttpRequestMessage req)
        {
            if (state.ContainsKey(StardustTimerKey)) return;
            state.SetState(StardustTimerKey, Stopwatch.StartNew());
        }

        protected override void DoGetHeader(IStateContainer state, HttpResponseMessage response)
        {
            var sw = state.GetState<Stopwatch>(StardustTimerKey);
            if (sw == null) return;
            sw.Stop();
            IEnumerable<string> values = response.Headers.GetValues("x-execution-timer");
            string s = values != null ? values.FirstOrDefault() : null;
            if (string.IsNullOrWhiteSpace(s))
                return;
            long num1 = long.Parse(s);
            long num2 = sw.ElapsedMilliseconds - num1;
            state.Extras.Add("latency", (object)num2);
            state.Extras.Add("serverTime", (object)num1);
            state.Extras.Add("totalTime", (object)sw.ElapsedMilliseconds);

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