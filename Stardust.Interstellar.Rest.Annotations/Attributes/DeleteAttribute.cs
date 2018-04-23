using System;

namespace Stardust.Interstellar.Rest.Annotations
{
    [AttributeUsage(AttributeTargets.Method)]
    public class DeleteAttribute : VerbAttribute
    {
        public DeleteAttribute() : base("DELETE")
        {
        }

        public DeleteAttribute(string route) : base("DELETE",route)
        {
        }
    }
}