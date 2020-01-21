using System;
using System.Collections.Generic;
using System.Web.Http.Dependencies;
using Microsoft.Extensions.DependencyInjection;
using Stardust.Particles;
using Stardust.Particles.Abstractions;
using IDependencyResolver = System.Web.Http.Dependencies.IDependencyResolver;

namespace Stardust.Interstellar.Rest.Dependencyinjection
{
    public class InterstellarDependencyResolver : IDependencyResolver, System.Web.Mvc.IDependencyResolver,IServiceProvider
    {
        public IServiceCollection Services { get; }
        private readonly IServiceProvider _serviceProvider;
        private IServiceScope _scope;

        public InterstellarDependencyResolver(IServiceCollection services)
        {
            _serviceProvider = services.BuildServiceProvider();
            Services = services;
        }

        private InterstellarDependencyResolver(IServiceScope serviceProvider)
        {
            _scope = serviceProvider;
            _serviceProvider = serviceProvider.ServiceProvider;
        }

        public void Dispose()
        {
            _scope.TryDispose();
        }

        public object GetService(Type serviceType)
        {
            return _serviceProvider.GetService(serviceType);
        }

        public IEnumerable<object> GetServices(Type serviceType)
        {
            return _serviceProvider.GetServices(serviceType);
        }

        public IDependencyScope BeginScope()
        {
            _serviceProvider.CreateScope();
            return new InterstellarDependencyResolver(_serviceProvider.CreateScope());
        }
    }
}