using System;

namespace Stardust.Interstellar.Rest.Annotations
{
    [AttributeUsage(AttributeTargets.Method)]
    public class PatchAttribute : VerbAttribute
    {
        public PatchAttribute() : base("PATCH")
        {
        }

        public PatchAttribute(string route) : base("PATCH", route)
        {
        }
    }
}