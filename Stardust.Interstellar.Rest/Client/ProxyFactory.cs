using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Stardust.Interstellar.Rest.Common;
using Stardust.Interstellar.Rest.Extensions;

namespace Stardust.Interstellar.Rest.Client
{
    public interface IProxyFactory
    {
        T CreateInstance<T>(string baseUrl);

        T CreateInstance<T>(string baseUrl, Action<Dictionary<string, object>> extrasCollector);

        object CreateInstance(Type interfaceType, string baseUrl);

        object CreateInstance(Type interfaceType, string baseUrl, Action<Dictionary<string, object>> extrasCollector);
    }

    public sealed class ProxyFactoryImplementation : IProxyFactory
    {
        private readonly IServiceLocator _serviceLocator;

        public ProxyFactoryImplementation(IServiceLocator serviceLocator)
        {
            _serviceLocator = serviceLocator;
        }
        public T CreateInstance<T>(string baseUrl)
        {
            return ProxyFactory.CreateInstance<T>(baseUrl, _serviceLocator);
        }

        public T CreateInstance<T>(string baseUrl, Action<Dictionary<string, object>> extrasCollector)
        {
            return ProxyFactory.CreateInstance<T>(baseUrl, extrasCollector, _serviceLocator);
        }

        public object CreateInstance(Type interfaceType, string baseUrl)
        {
            return ProxyFactory.CreateInstance(interfaceType, baseUrl, _serviceLocator);
        }

        public object CreateInstance(Type interfaceType, string baseUrl, Action<Dictionary<string, object>> extrasCollector)
        {
            return ProxyFactory.CreateInstance(interfaceType, baseUrl, extrasCollector, _serviceLocator);
        }
    }

    public static class ProxyFactory
    {
        public static T CreateRestClient<T>(this IServiceProvider serviceLocator, string baseUrl)
        {
            return serviceLocator.GetService<IProxyFactory>().CreateInstance<T>(baseUrl);
        }

        public static T CreateRestClient<T>(this IServiceProvider serviceLocator, string baseUrl, Action<Dictionary<string, object>> extrasCollector)
        {
            return serviceLocator.GetService<IProxyFactory>().CreateInstance<T>(baseUrl, extrasCollector);
        }

        

        static ConcurrentDictionary<Type, Type> proxyTypeCache = new ConcurrentDictionary<Type, Type>();

        public static bool EnableExpectContinue100ForPost { get; set; }

        public static bool EnableExpectContinue100ForAll { get; set; }
        public static bool RunAuthProviderBeforeAppendingBody { get; set; }

        public static Type CreateProxy<T>()
        {
            var interfaceType = typeof(T);
            return CreateProxy(interfaceType);
        }

        public static Type CreateProxy(Type interfaceType)
        {
            Type type;
            if (proxyTypeCache.TryGetValue(interfaceType, out type)) return type;
            lock (interfaceType)
            {
                if (proxyTypeCache.TryGetValue(interfaceType, out type)) return type;
                var builder = new ProxyBuilder(interfaceType);
                var newType = builder.Build();
                if (proxyTypeCache.TryGetValue(interfaceType, out type)) return type;
                proxyTypeCache.TryAdd(interfaceType, newType);
                return newType;
            }
        }

        internal static T CreateInstance<T>(string baseUrl, IServiceProvider serviceLocator)
        {
            return CreateInstance<T>(baseUrl, null, serviceLocator);
        }

        internal static T CreateInstance<T>(string baseUrl, Action<Dictionary<string, object>> extrasCollector, IServiceProvider serviceLocator)
        {
            return (T)CreateInstance(typeof(T), baseUrl, extrasCollector, serviceLocator);
        }

        internal static object CreateInstance(Type interfaceType, string baseUrl, IServiceProvider serviceLocator)
        {
            return CreateInstance(interfaceType, baseUrl, null, serviceLocator);
        }
        internal static object CreateInstance(Type interfaceType, string baseUrl, Action<Dictionary<string, object>> extrasCollector, IServiceProvider serviceLocator)
        {
            var t = CreateProxy(interfaceType);
            var auth = interfaceType.GetCustomAttributes().SingleOrDefault(a => a is IAuthenticationInspector) as IAuthenticationInspector;
            var authHandler = GetAuthenticationHandler(auth, serviceLocator);
            var instance = ActivatorUtilities.CreateInstance(serviceLocator, t, authHandler, new HeaderHandlerFactory(interfaceType), TypeWrapper.Create(interfaceType));
            ((RestWrapper)instance).Extras = extrasCollector;
            var i = (RestWrapper)instance;
            i.SetBaseUrl(baseUrl);
            return instance;
        }

        private static object GetAuthenticationHandler(IAuthenticationInspector auth, IServiceProvider serviceLocator)
        {
            IAuthenticationHandler authHandler;
            if (auth == null)
            {
                authHandler = serviceLocator.GetService<IAuthenticationHandler>() ?? new NullAuthHandler();
            }
            else
            {
                authHandler = auth.GetHandler(serviceLocator);
            }
            return authHandler;
        }

    }
}