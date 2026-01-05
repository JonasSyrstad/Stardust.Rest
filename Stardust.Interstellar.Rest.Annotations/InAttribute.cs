using System;

namespace Stardust.Interstellar.Rest.Annotations
{
    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
    public class InAttribute : Attribute
    {

        public InAttribute(InclutionTypes InclutionType)
        {
            this.InclutionType = InclutionType;
        }

        public InAttribute()
        {
            InclutionType = InclutionTypes.Path;
        }

        public InclutionTypes InclutionType { get; set; }

        /// <summary>
        /// Gets or sets the custom name to use for the parameter in requests/responses.
        /// When set, this name will be used instead of the C# parameter name for query strings and headers.
        /// </summary>
        public string Name { get; set; }
    }
}