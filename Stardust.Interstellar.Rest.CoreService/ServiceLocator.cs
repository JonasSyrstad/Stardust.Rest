using System;
using System.Collections.Generic;
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
    public sealed class ServiceLocator : IServiceLocator
    {
        private readonly IServiceProvider _serviceProvider;

        public ServiceLocator(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public T GetService<T>()
        {
            return _serviceProvider.GetService<T>();
        }

        public object GetService(Type serviceType)
        {
            return _serviceProvider.GetService(serviceType);
        }

        public IEnumerable<T> GetServices<T>()
        {
            return _serviceProvider.GetServices<T>();
        }

        public object CreateInstanceOf(Type type) => ActivatorUtilities.CreateInstance(_serviceProvider, type);

        public T CreateInstance<T>() where T : class => ActivatorUtilities.CreateInstance(_serviceProvider, typeof(T)) as T;
    }
}