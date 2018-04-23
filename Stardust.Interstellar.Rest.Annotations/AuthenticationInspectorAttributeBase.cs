using System;
using Stardust.Interstellar.Rest.Extensions;

namespace Stardust.Interstellar.Rest.Annotations
{
    [AttributeUsage(AttributeTargets.Interface)]
    public abstract class AuthenticationInspectorAttributeBase : Attribute, IAuthenticationInspector
    {
        public abstract IAuthenticationHandler GetHandler();
    }
}