using System;
using Stardust.Interstellar.Rest.Annotations;

namespace Stardust.Interstellar.Rest.Common
{
    public class ParameterWrapper
    {
        /// <summary>
        /// Gets or sets the C# parameter name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the custom name to use for the parameter in requests/responses.
        /// When null or empty, the Name property will be used.
        /// </summary>
        public string CustomName { get; set; }

        /// <summary>
        /// Gets the effective name to use for the parameter (CustomName if set, otherwise Name).
        /// </summary>
        public string EffectiveName => !string.IsNullOrWhiteSpace(CustomName) ? CustomName : Name;

        /// <summary>
        /// Gets or sets the description for the parameter to be used in generated documentation.
        /// </summary>
        public string Description { get; set; }

        public object value { get; set; }

        public InclusionTypes In { get; set; }

        public Type Type { get; set; }
        

        public ParameterWrapper Create(object value)
        {
           return new ParameterWrapper { value = value, Type = Type, In = In, Name = Name, CustomName = CustomName, Description = Description };
        }
    }
}