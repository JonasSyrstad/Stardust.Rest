using System;

namespace Stardust.Interstellar.Rest.Annotations.Messaging
{
    [AttributeUsage(AttributeTargets.Parameter)]
    public class ExtensionLevelAttribute : Attribute
    {
        public int PropertInjectionLevel { get; set; }

        public ExtensionLevelAttribute()
        {
            PropertInjectionLevel = 0;
        }
        public ExtensionLevelAttribute(int propertInjectionLevel)
        {
            PropertInjectionLevel = propertInjectionLevel;
        }
    }
}