using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stardust.Interstellar.Rest.Annotations
{
    [AttributeUsage(AttributeTargets.Interface | AttributeTargets.Method,AllowMultiple = false)]
    public class ServiceDescriptionAttribute : Attribute
    {
        private string description;

        /// <summary>Initializes a new instance of the <see cref="T:System.Attribute" /> class.</summary>
        public ServiceDescriptionAttribute(string description)
        {
            this.description = description;
        }

        public string Description
        {
            get
            {
                return description;
            }
            set
            {
                description = value;
            }
        }

        public string Tags { get; set; }

        public string Summary { get; set; }

        public bool IsDeprecated { get; set; }

        public string Responses { get; set; }
    }
}
