using Microsoft.Extensions.DependencyInjection;
using Owin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Web.Http;
using System.Web.Mvc;

namespace Stardust.Interstellar.Rest.Dependencyinjection
{
    public static class ServiceProviderExtensions
    {

        public static IServiceCollection AddAllControllersFrom(this IServiceCollection services, Assembly assembly)
        {
            services.AddControllersAsServices(assembly.GetExportedTypes()
                .Where(t => !t.IsAbstract && !t.IsGenericTypeDefinition)
                .Where(t => typeof(IController).IsAssignableFrom(t)
                            || t.Name.EndsWith("Controller", StringComparison.OrdinalIgnoreCase)));
            return services;
        }

        public static IServiceCollection AddAllControllersInContaingAssebly<T>(this IServiceCollection services)
        {
            return services.AddAllControllersFrom(typeof(T).Assembly);
        }
        public static IAppBuilder AddDependencyInjection<T>(this IAppBuilder app, ControllerTypes addFor=ControllerTypes.Both) where T : ServicesConfiguration, new()
        {
            var services = new T().ConfigureInterstellar(new ServiceCollection());
            var appAssembly = app.GetType().Assembly;
            var configurationAssembly = typeof(T).Assembly;
            services.AddAllControllersFrom(appAssembly);
            if (appAssembly != configurationAssembly)
                services.AddAllControllersFrom(configurationAssembly);
            var resolver = new InterstellarDependencyResolver(services);
            if (addFor == ControllerTypes.Both || addFor == ControllerTypes.Mvc)
                DependencyResolver.SetResolver(resolver);
            if (addFor == ControllerTypes.Both || addFor == ControllerTypes.WebApi)
                GlobalConfiguration.Configuration.DependencyResolver = resolver;
            return app;
        }
        public static IServiceCollection AddControllersAsServices(this IServiceCollection services,
            IEnumerable<Type> controllerTypes)
        {
            foreach (var type in controllerTypes)
            {
                services.AddTransient(type);
            }

            return services;
        }
    }
}
