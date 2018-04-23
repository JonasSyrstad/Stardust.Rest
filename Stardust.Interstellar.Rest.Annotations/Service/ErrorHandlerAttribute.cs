using System;
using Microsoft.Extensions.DependencyInjection;
using Stardust.Interstellar.Rest.Extensions;

namespace Stardust.Interstellar.Rest.Service
{
    [AttributeUsage(AttributeTargets.Interface)]
    public sealed class ErrorHandlerAttribute : Attribute
    {
        /// <summary>Initializes a new instance of the <see cref="T:System.Attribute" /> class.</summary>
        public ErrorHandlerAttribute(IServiceLocator serviceLocator, Type errorHandlerType)
        {
            this.ErrorHandler = (IErrorHandler)serviceLocator?.CreateInstanceOf(errorHandlerType) ?? (IErrorHandler)ActivatorUtilities.CreateInstance(new InternalServiceProvider(serviceLocator),errorHandlerType);
        }

        public IErrorHandler ErrorHandler { get; set; }
    }

    internal class InternalServiceProvider : IServiceProvider
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