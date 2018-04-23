using System;
using System.Net;
using Stardust.Interstellar.Rest.Extensions;

namespace Stardust.Interstellar.Rest.Client
{
    public class ResultWrapper
    {
        public bool IsVoid { get; set; }

        public object Value { get; set; }

        public Type Type { get; set; }

        public HttpStatusCode Status { get; set; }

        public string StatusMessage { get; set; }

        public Exception Error { get; set; }

        internal string ActionId { get; set; }

        public void EndState()
        {
            StateHelper.EndState(ActionId);
        }
    }
}