using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Stardust.Interstellar.Rest.Annotations;
using Stardust.Interstellar.Rest.Client;
using Stardust.Interstellar.Rest.Common;
using Stardust.Interstellar.Rest.Extensions;

namespace Stardust.Interstellar.Rest.Service
{
    public static class MvcExtensions
    {
        public static IServiceCollection AddInterstellar(this IServiceCollection services)
        {
            return services.AddInterstellar(false);
        }
        public static IServiceCollection AddInterstellar(this IServiceCollection services, bool disableVersionHeader)
        {
            services.AddScoped<IServiceLocator>(s => new Locator(s))
                .AddScoped<IProxyFactory, ProxyFactoryImplementation>()
                .AddSingleton<IWebMethodConverter, VerbResolver>();
            if (disableVersionHeader) ServiceFactory.DisableStardustVersion();
            return services;
        }



        internal sealed class VerbResolver : IWebMethodConverter
        {

            public List<HttpMethod> GetHttpMethods(MethodInfo method)
            {
                var verbs = method.GetCustomAttributes<VerbAttribute>();
                return verbs.Select(verb => new HttpMethod(verb?.Verb ?? "GET")).ToList();
            }
        }
    }
}