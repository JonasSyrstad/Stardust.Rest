using System;

namespace Stardust.Interstellar.Rest.Annotations
{
    [AttributeUsage(AttributeTargets.Method)]
    public class PatchAttribute : VerbAttribute
    {
        public PatchAttribute() : base("PATHC")
        {
        }

        public PatchAttribute(string route) : base("PATHC",route)
        {
        }
    }
}