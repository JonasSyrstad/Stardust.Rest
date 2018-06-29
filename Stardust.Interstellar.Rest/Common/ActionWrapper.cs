using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using Stardust.Interstellar.Rest.Annotations;
using Stardust.Interstellar.Rest.Annotations.Service;
using Stardust.Interstellar.Rest.Extensions;
using Stardust.Interstellar.Rest.Service;

namespace Stardust.Interstellar.Rest.Common
{
    public class ActionWrapper
    {
        public string Name { get; set; }

        public Type ReturnType { get; set; }

        public List<ParameterWrapper> Parameters { get; set; }

        public string RouteTemplate { get; set; }

        public List<HttpMethod> Actions { get; set; }

        public List<IHeaderInspector> CustomHandlers { get; set; }

        public InputInterceptorAttribute[] Interceptor { get; set; }

        public bool UseXml { get; set; }

        public int MessageExtesionLevel { get; set; }
        public bool Retry { get; set; }
        public int Interval { get; set; }
        public int NumberOfRetries { get; set; }
        public bool IncrementalRetry { get; set; }
        public IErrorCategorizer ErrorCategorizer { get; set; }
        public List<ServiceInitializerAttribute> Initializers { get; set; }
        //public IErrorHandler ErrorHandler { get; set; }
        public IThrottlingManager Throttler { get; set; }
        public ErrorHandlerAttribute ErrorHandler { get; set; }
        public IErrorHandler DefaultErrorHandler { get; set; }
    }


}