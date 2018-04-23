using System;

namespace Stardust.Interstellar.Rest.Annotations
{
    public class IRouteAttribute : Attribute
    {
        public IRouteAttribute(string template)
        {
            Template = template;
        }

        public string Template { get; set; }
    }

    [AttributeUsage(AttributeTargets.Interface)]
    public class IRoutePrefixAttribute : Attribute
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