using System;
using Stardust.Interstellar.Rest.Extensions;

namespace Stardust.Interstellar.Rest.Client
{
    public class InternalServiceProvider : IServiceProvider
    {
        private readonly IServiceLocator _serviceLocator;

        public InternalServiceProvider(IServiceLocator serviceLocator)
        {
            _serviceLocator = serviceLocator;
        }

        public object GetService(Type serviceType)
        {
            return _serviceLocator.GetService(serviceType);
        }
    }
}