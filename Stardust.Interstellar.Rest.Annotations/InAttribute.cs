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
    }
}