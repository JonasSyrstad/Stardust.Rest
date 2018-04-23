using System;
using Stardust.Interstellar.Rest.Annotations;

namespace Stardust.Interstellar.Rest.Common
{
    public class ParameterWrapper
    {
        public string Name { get; set; }

        public object value { get; set; }

        public InclutionTypes In { get; set; }

        public Type Type { get; set; }
        

        public ParameterWrapper Create(object value)
        {
           return new ParameterWrapper { value = value, Type = Type, In = In, Name = Name};
        }
    }
}