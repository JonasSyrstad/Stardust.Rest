using Microsoft.Extensions.DependencyInjection;
using Stardust.Interstellar.Rest.Annotations.Resolver;
using Stardust.Interstellar.Rest.Client;
using Stardust.Interstellar.Rest.Extensions;

namespace Stardust.Interstellar.Rest.Common
{
    public static class DependencyInjectionConfiguration
    {
        /// <summary>
        /// Enable Interstellar rest client generator
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IServiceCollection AddInterstellarClient(this IServiceCollection services)
        {
            return services.AddScoped<IProxyFactory, ProxyFactoryImplementation>()
                  .AddScoped<IServiceLocator, Locator>();
        }

        public static IServiceCollection AddInterstellarClient<T>(this IServiceCollection services, string baseUrl)
        {
            return services.AddScoped(typeof(T),s => s.CreateRestClient<T>(baseUrl));
        }
    }
}
