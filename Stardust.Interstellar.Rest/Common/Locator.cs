using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Stardust.Interstellar.Rest.Extensions;
using Stardust.Particles.Abstractions;

namespace Stardust.Interstellar.Rest.Common
{
    public class Locator : IServiceLocator
    {
        private readonly IServiceProvider _serviceProvider;

        public Locator(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public object GetService(Type serviceType)
        {
            return _serviceProvider.GetService(serviceType);
        }

        public T GetService<T>()
        {
            var service = GetService(typeof(T));
            if (service == null) return default(T);
            return (T)service;
        }

        public IEnumerable<T> GetServices<T>()
        {
            try
            {
                if (_serviceProvider is IServiceLocator sl) return sl.GetServices<T>();
                if (_serviceProvider is IDependencyResolver dr) return dr.GetServices<T>();
                return _serviceProvider.GetServices<T>();
            }
            catch (Exception)
            {
                return new List<T>();
            }
        }

        public object CreateInstanceOf(Type type)
        {
            return ActivatorUtilities.CreateInstance(_serviceProvider, type);
        }

        public T CreateInstance<T>() where T : class
        {
            return ActivatorUtilities.CreateInstance(_serviceProvider, typeof(T)) as T;
        }
    }
}