using System;
using Microsoft.Extensions.DependencyInjection;

namespace Stardust.Interstellar.Rest.Service
{
	internal class InternalServiceProvider : IServiceProvider
	{
		private readonly IServiceProvider _serviceLocator;

		public InternalServiceProvider(IServiceProvider serviceLocator)
		{
			_serviceLocator = serviceLocator;
		}

		public object GetService(Type serviceType)
		{
			return ActivatorUtilities.CreateInstance(_serviceLocator, serviceType);
		}
	}
}