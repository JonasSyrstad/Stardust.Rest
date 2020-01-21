using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using Stardust.Interstellar.Rest.Annotations.Rest.Extensions;

namespace Stardust.Interstellar.Rest.Client
{
    public static class ServiceConfigurationExtensions
    {
        public static T SetProxy<T>(this T service, IWebProxy serviceProxy) where T : IConfigurableService
        {
            var client = service as RestWrapper;
            client?.SetProxyHandler(s => serviceProxy);
            return service;
        }

        public static T SetProxy<T>(this T service, Func<string, IWebProxy> serviceProxyFunc) where T : IConfigurableService
        {
            var client = service as RestWrapper;
            client?.SetProxyHandler(serviceProxyFunc);
            return service;
        }

        public static T SetQueryStringVersion<T>(this T service, string versionKey, string version) where T: IConfigurableService
        {
            var client = service as RestWrapper;
            client?.AppendTrailingQueryString($"{versionKey}={version}");
            return service;
        }

        public static T SetHeaderVersion<T>(this T service, string versionKey, string version) where T : IConfigurableService
        {
            var client = service as RestWrapper;
            client?.SetHttpHeader(versionKey,version);
            return service;
        }

        public static T SetVersion<T>(this T service, string version) where T : IConfigurableService
        {
            var client = service as RestWrapper;
            client?.SetPathVersion(version);
            return service;
        }

        public static T AddHeaderValue<T>(this T service, string headerName, string headerValue)
            where T : IConfigurableService
        {
            var client = service as RestWrapper;
            client?.SetHttpHeader(headerName,headerValue);
            return service;
        }

        public static T RunAuthProviderBeforeAppendingBody<T>(this T service,bool runFirst=true)
            where T : IConfigurableService
        {
            var client = service as RestWrapper;
            if (client != null) client.RunAuthProviderBeforeAppendingBody = runFirst;
            return service;
        }

        public static T AddHeaderValues<T>(this T service, IDictionary<string,string> headers)
            where T : IConfigurableService
        {
            foreach (var header in headers)
            {
                service.AddHeaderValue(header.Key, header.Value);
            }
            return service;
        }
    }
}
