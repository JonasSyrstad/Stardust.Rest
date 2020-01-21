using Microsoft.Extensions.DependencyInjection;
using Owin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using Stardust.Interstellar.Rest.Dependencyinjection;
using Stardust.Particles;


namespace Stardust.Interstellar.Rest.DependencyInjection
{
    public static class ServiceProviderExtensions
    {
        internal static Func<HttpContextBase> CurrentHttpContext = () => new HttpContextWrapper(HttpContext.Current);
        public static IAppBuilder AddDependencyInjection(this IAppBuilder app, string apiPath = "/api", bool addMvcDi = true)
        {
            if (_addedToOwin) return app;
            if (apiPath.ContainsCharacters())
            {
                app.Map(apiPath, api =>
              {
                  var config = new HttpConfiguration();
                  config.MapHttpAttributeRoutes();
                  config.DependencyResolver = _resolver;

                  api.UseWebApi(config);
              });
            }

            
            return app;
        }

        private static InterstellarDependencyResolver _resolver;
        private static bool _addedToOwin;

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

        public static HttpApplication AddDependencyInjection<T>(this HttpApplication app,
            ControllerTypes addFor = ControllerTypes.Both) where T : ServicesConfiguration, new()
        {
            return app.AddDependencyInjectionInternal<T>(addFor) as HttpApplication;
        }

        public static IAppBuilder AddDependencyInjection<T>(this IAppBuilder app,
            ControllerTypes addFor = ControllerTypes.Both) where T : ServicesConfiguration, new()
        {
            return app.AddDependencyInjectionInternal<T>(addFor) as IAppBuilder;
        }

        private static object AddDependencyInjectionInternal<T>(this object app, ControllerTypes addFor = ControllerTypes.Both) where T : ServicesConfiguration, new()
        {
            var services = new T().ConfigureInterstellar(new ServiceCollection());
            var appAssembly = app.GetType().Assembly;
            var configurationAssembly = typeof(T).Assembly;
            services.AddAllControllersFrom(appAssembly);
            if (appAssembly != configurationAssembly)
                services.AddAllControllersFrom(configurationAssembly);
            _resolver = new InterstellarDependencyResolver(services);
            if (addFor == ControllerTypes.Both || addFor == ControllerTypes.Mvc)
                DependencyResolver.SetResolver(_resolver);
            if (addFor == ControllerTypes.Both || addFor == ControllerTypes.WebApi)
                GlobalConfiguration.Configuration.DependencyResolver = _resolver;
            if (app is IAppBuilder appBuilder)
            {
                appBuilder.AddDependencyInjection();
            }
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
