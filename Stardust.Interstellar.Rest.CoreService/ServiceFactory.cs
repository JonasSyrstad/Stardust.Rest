using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Stardust.Interstellar.Rest.Annotations;
using Stardust.Interstellar.Rest.Client;
using Stardust.Interstellar.Rest.Common;
using Stardust.Interstellar.Rest.Extensions;

namespace Stardust.Interstellar.Rest.Service
{
    public static class ServiceFactory
    {
        internal static bool _includeStardustVersion = true;
        public static void DisableStardustVersion()
        {
            _includeStardustVersion = false;
        }
        public static IMvcBuilder AddAsController<TService, TImplementation>(this IMvcBuilder builder) where TService : class
            where TImplementation : class, TService
        {
            var service = CreateServiceImplementation<TService>(new Locator(builder.Services.BuildServiceProvider()));
            builder.Services.AddScoped<TService, TImplementation>();
            return builder;
        }

        public static IMvcBuilder AddAsController<TService>(this IMvcBuilder builder, Type implementationType) where TService : class
        {
            var service = CreateServiceImplementation<TService>(new Locator(builder.Services.BuildServiceProvider()));
            builder.Services.AddScoped(typeof(TService), implementationType);
            return builder;
        }

        public static IMvcBuilder AddProxyController<TService>(this IMvcBuilder builder, string baseUrl) where TService : class
        {
            var service = CreateServiceImplementation<TService>(new Locator(builder.Services.BuildServiceProvider()));
            builder.Services.AddScoped(s => s.CreateRestClient<TService>(baseUrl));
            return builder;
        }

        public static IMvcBuilder AddAsController<TService>(this IMvcBuilder builder) where TService : class

        {
            var service = CreateServiceImplementation<TService>(new Locator(builder.Services.BuildServiceProvider()));
            return builder;
        }

        public static IMvcBuilder AddAsController<TService>(this IMvcBuilder builder, Func<IServiceProvider, TService> func) where TService : class
        {
            var service = CreateServiceImplementation<TService>(new Locator(builder.Services.BuildServiceProvider()));
            builder.Services.AddScoped(func);
            return builder;
        }
        public static IMvcBuilder UseInterstellar(this IMvcBuilder builder)
        {
            builder.AddApplicationPart(GetAssembly());
            return builder;
        }
        static ServiceFactory()
        {
            AuthorizeWrapperAttribute.SetAttributeCreator(a => new AuthorizeAttribute { Roles = a.Roles, AuthenticationSchemes = "OAuth2", Policy = a.Policy });
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="services"></param>
        /// <param name="authenticationSchemes">sets a comma delimited list of schemes from which user information is constructed.</param>
        /// <returns></returns>
        public static IServiceCollection SetAuthenticationSchemes(this IServiceCollection services, string authenticationSchemes)
        {
            AuthorizeWrapperAttribute.SetAttributeCreator(a => new AuthorizeAttribute { Roles = a.Roles, AuthenticationSchemes = authenticationSchemes });
            return services;
        }
        static ServiceBuilder Builder = new ServiceBuilder();

        private static List<Type> ServiceTypes = new List<Type>();

        public static Type CreateServiceImplementation<T>(IServiceLocator serviceLocator)
        {
            var type = Builder.CreateServiceImplementation<T>(serviceLocator);
            if (type != null)
                ServiceTypes.Add(type);
            return type;
        }

        public static IEnumerable<Type> CreateServiceImplementationForAllInCotainingAssembly<T>(
            IServiceLocator serviceLocator)
        {
            var assembly = typeof(T).Assembly;
            return CreateServiceImplementations(assembly, serviceLocator);
        }
        public static IEnumerable<Type> CreateServiceImplementationForAllInCotainingAssembly<T>(IApplicationBuilder builder)
        {
            var assembly = typeof(T).Assembly;
            return CreateServiceImplementations(assembly, new Locator(builder.ApplicationServices));
        }

        public static IEnumerable<Type> CreateServiceImplementations(Assembly assembly, IServiceLocator serviceLocator)
        {
            var types = assembly.GetTypes().Where(t => t.IsInterface).Select(item => Builder.CreateServiceImplementation(item, serviceLocator));
            ServiceTypes.AddRange(types.Where(i => i != null));
            return types;
        }

        public static void FinalizeRegistration()
        {

            //try
            //{
            //    Builder.Save();
            //}
            //catch (Exception)
            //{
            //    // ignored
            //}

            //var parent = (IHttpControllerTypeResolver)GlobalConfiguration.Configuration.Services.GetService(typeof(IHttpControllerTypeResolver));
            //CustomAssebliesResolver.SetParent(parent);
            //GlobalConfiguration.Configuration.Services.Replace(typeof(IHttpControllerTypeResolver), new CustomAssebliesResolver());
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
}

