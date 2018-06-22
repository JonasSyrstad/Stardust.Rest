using System;

namespace Stardust.Interstellar.Rest.Annotations
{
    /// <summary>
    /// Adds support for Microsoft.AspNetCore.Mvc.Versioning on the generated controller. See https://github.com/Microsoft/aspnet-api-versioning/wiki for details on configuring your app for versioning.
    /// </summary>
    [AttributeUsage(AttributeTargets.Interface)]
    public class VersionAttribute : Attribute
    {
        public VersionAttribute()
        {
            
        }
        public VersionAttribute(string version)
        {
            Version = version;
        }


        public string Version { get; set; }

        public bool Deprecated { get; set; }
    }
}