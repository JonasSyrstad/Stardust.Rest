using System;

namespace Stardust.Interstellar.Rest.Annotations
{
    public interface IRoute
    {
        string Template { get; set; }
    }

    [Obsolete("Put the route information in the Verb attribute",false)]
    public class IRouteAttribute : Attribute,IRoute
    {
        public IRouteAttribute(string template)
        {
            Template = template;
        }

        public string Template { get; set; }
    }

    /// <summary>
    /// Tells the code generation tools to create a webapi controller for this interface
    /// </summary>
    [AttributeUsage(AttributeTargets.Interface)]
    public class ApiAttribute : Attribute, IRoutePrefix
    {
        private readonly string prefix;

        private readonly bool includeTypeName;

        public ApiAttribute(string prefix)
        {
            this.prefix = prefix;
        }

        public ApiAttribute(string prefix, bool includeTypeName)
        {
            this.prefix = prefix;
            this.includeTypeName = includeTypeName;
        }

        public string Prefix
        {
            get
            {
                return prefix;
            }
        }

        public bool IncludeTypeName
        {
            get
            {
                return includeTypeName;
            }
        }

    }

    public interface IRoutePrefix
    {
        string Prefix { get; }
        [Obsolete]
        bool IncludeTypeName { get; }
    }

    [Obsolete("Put the route prefix information in the ApiAttribute", false)]
    [AttributeUsage(AttributeTargets.Interface)]
    public class IRoutePrefixAttribute : Attribute,IRoutePrefix
    {
        private readonly string prefix;

        private readonly bool includeTypeName;

        public IRoutePrefixAttribute(string prefix)
        {
            this.prefix = prefix;
        }

        public IRoutePrefixAttribute(string prefix, bool includeTypeName)
        {
            this.prefix = prefix;
            this.includeTypeName = includeTypeName;
        }

        public string Prefix
        {
            get
            {
                return prefix;
            }
        }

        public bool IncludeTypeName
        {
            get
            {
                return includeTypeName;
            }
        }
    }
}