using System;
using Stardust.Interstellar.Rest.Extensions;

namespace Stardust.Interstellar.Rest.Annotations
{
    [AttributeUsage(AttributeTargets.Interface|AttributeTargets.Method | AttributeTargets.Assembly, AllowMultiple = true)]
    public abstract class HeaderInspectorAttributeBase : Attribute, IHeaderInspector
    {
        public abstract IHeaderHandler[] GetHandlers(IServiceProvider serviceLocator);
    }
}