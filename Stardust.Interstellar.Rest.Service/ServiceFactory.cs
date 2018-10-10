using Microsoft.Extensions.DependencyInjection;
using Stardust.Interstellar.Rest.Annotations;
using Stardust.Interstellar.Rest.Common;
using Stardust.Interstellar.Rest.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Web.Http;
using System.Web.Http.Dispatcher;

namespace Stardust.Interstellar.Rest.Service
{
    public static class ServiceFactory
    {
        public static IServiceCollection FinalizeRegistration(this IServiceCollection services)
        {
            FinalizeRegistration();
            return services;
        }
        public static IServiceCollection AddInterstellarServices(this IServiceCollection services)
        {
            return services.AddSingleton<IWebMethodConverter, VerbResolver>();
        }

        public static IServiceCollection AddAsController<TService, TImplementation>(this IServiceCollection services, IServiceProvider serviceProvider) where TImplementation : class, TService
        {
            var controller = CreateServiceImplementation<TService>(new Locator(serviceProvider));
            services.AddScoped(controller);
            services.AddScoped(typeof(TService),typeof(TImplementation));
            return services;
        }

        public static void DisableStardustVersion()
        {
            _includeStardustVersion = false;
        }
        static ServiceFactory()
        {
            AuthorizeWrapperAttribute.SetAttributeCreator(a => new AuthorizeAttribute { Roles = a.Roles, Users = a.Users });
        }

        private static ServiceBuilder Builder = new ServiceBuilder();

        private static List<Type> ServiceTypes = new List<Type>();
        internal static bool _includeStardustVersion = true;

        public static Type CreateServiceImplementation<T>(IServiceLocator serviceLocator)
        {
            var type = Builder.CreateServiceImplementation<T>(serviceLocator);
            if (type != null)
                ServiceTypes.Add(type);
            return type;
        }

        public static IEnumerable<Type> CreateServiceImplementationForAllInCotainingAssembly<T>(IServiceLocator serviceLocator)
        {
            var assembly = typeof(T).Assembly;
            return CreateServiceImplementations(assembly, serviceLocator);
        }

        public static IEnumerable<Type> CreateServiceImplementations(Assembly assembly, IServiceLocator serviceLocator)
        {
            var types = assembly.GetTypes().Where(t => t.IsInterface).Select(item => Builder.CreateServiceImplementation(item, serviceLocator));
            ServiceTypes.AddRange(types.Where(i => i != null));
            return types;
        }

        public static void FinalizeRegistration()
        {

            try
            {
                Builder.Save();
            }
            catch (Exception)
            {
                // ignored
            }
            var parent = (IHttpControllerTypeResolver)GlobalConfiguration.Configuration.Services.GetService(typeof(IHttpControllerTypeResolver));
            CustomAssebliesResolver.SetParent(parent);
            GlobalConfiguration.Configuration.Services.Replace(typeof(IHttpControllerTypeResolver), new CustomAssebliesResolver());
        }

        public static Assembly GetAssembly()
        {
            return Builder.GetCustomAssembly();
        }

        public static IEnumerable<Type> GetTypes()
        {
            return ServiceTypes;
        }
    }

    public class VerbResolver : IWebMethodConverter
    {
        public List<HttpMethod> GetHttpMethods(MethodInfo method)
        {
            var verbs = method.GetCustomAttributes<VerbAttribute>();
            return verbs.Select(verb => new HttpMethod(verb?.Verb ?? "GET")).ToList();
        }
    }
}

