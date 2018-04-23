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

    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
    public class InPathAttribute : InAttribute
    {
        public InPathAttribute():base(InclutionTypes.Path)
        {
            
        }
    }

    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
    public class InQueryAttribute : InAttribute
    {
        public InQueryAttribute() : base(InclutionTypes.Query)
        {

        }
    }

    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
    public class InHeaderAttribute : InAttribute
    {
        public InHeaderAttribute() : base(InclutionTypes.Header)
        {

        }
    }
    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
    public class InBodyAttribute : InAttribute
    {
        public InBodyAttribute() : base(InclutionTypes.Body)
        {

        }
    }
}