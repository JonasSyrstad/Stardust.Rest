using System;
using Stardust.Interstellar.Rest.Extensions;

namespace Stardust.Interstellar.Rest.Annotations
{
    [AttributeUsage(AttributeTargets.Interface|AttributeTargets.Method | AttributeTargets.Assembly, AllowMultiple = true)]
    public abstract class HeaderInspectorAttributeBase : Attribute, IHeaderInspector
    {
        public abstract IHeaderHandler[] GetHandlers(IServiceLocator serviceLocator);
    }

    /// <summary>
    /// Causes the generated client to use xml as messaging format.
    /// </summary>
    [AttributeUsage(AttributeTargets.Interface|AttributeTargets.Method, AllowMultiple = true)]
    public class UseXmlAttribute : Attribute
    {
        
    }
}