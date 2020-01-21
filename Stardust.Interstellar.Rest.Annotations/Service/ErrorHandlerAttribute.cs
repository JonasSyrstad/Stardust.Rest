using System;
using Microsoft.Extensions.DependencyInjection;
using Stardust.Interstellar.Rest.Extensions;

namespace Stardust.Interstellar.Rest.Service
{
    [AttributeUsage(AttributeTargets.Interface)]
    public sealed class ErrorHandlerAttribute : Attribute
    {
        private readonly Type _errorHandlerType;

        /// <summary>Initializes a new instance of the <see cref="T:System.Attribute" /> class.</summary>
        public ErrorHandlerAttribute(Type errorHandlerType)
        {
            _errorHandlerType = errorHandlerType;
            //this.ErrorHandler = (IErrorHandler)serviceLocator?.CreateInstanceOf(errorHandlerType) ?? (IErrorHandler)ActivatorUtilities.CreateInstance(new InternalServiceProvider(serviceLocator),errorHandlerType);
        }

        public IErrorHandler ErrorHandler(IServiceProvider locator)
        {
            return ActivatorUtilities.CreateInstance(locator, _errorHandlerType) as IErrorHandler;
        }
    }
}