using Microsoft.Extensions.DependencyInjection;
using Stardust.Interstellar.Rest.Client;
using Stardust.Interstellar.Rest.Extensions;

namespace Stardust.Interstellar.Rest.Service
{
    public static class MvcExtensions
    {
        public static IServiceCollection AddInterstellar(this IServiceCollection services)
        {
            services.AddScoped<IServiceLocator>(s => new ServiceLocator(s))
                .AddScoped<IProxyFactory,ProxyFactoryImplementation>();
            return services;
        }
    }
}