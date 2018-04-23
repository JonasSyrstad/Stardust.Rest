using System;

namespace Stardust.Interstellar.Rest.Extensions
{
    public abstract class InputInterceptorAttribute : Attribute
    {
        public abstract IInputInterceptor GetInterceptor(IServiceLocator serviceLocator);
    }
}