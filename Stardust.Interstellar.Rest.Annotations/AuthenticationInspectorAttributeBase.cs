using Stardust.Interstellar.Rest.Extensions;
using System;

namespace Stardust.Interstellar.Rest.Annotations
{
    [AttributeUsage(AttributeTargets.Interface)]
    public abstract class AuthenticationInspectorAttributeBase : Attribute, IAuthenticationInspector
    {
        public abstract IAuthenticationHandler GetHandler(IServiceProvider provider);
    }
}