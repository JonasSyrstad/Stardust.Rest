using System;
using Stardust.Interstellar.Rest.Extensions;

namespace Stardust.Interstellar.Rest.Annotations
{
    /// <summary>
    /// Adds the elapsed time in ms to the http headers (x-execution-timer) on the server side and calculates total execuition time and network latency on the client side
    /// </summary>
    [AttributeUsage(AttributeTargets.Interface | AttributeTargets.Method|AttributeTargets.Assembly)]
    public class PerformanceHeadersAttribute : HeaderInspectorAttributeBase
    {
        public override IHeaderHandler[] GetHandlers(IServiceProvider serviceLocator)
        {
            return new IHeaderHandler[] { new PerformanceHeadersHandler() };
        }
    }
}
