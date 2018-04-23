using System;

namespace Stardust.Interstellar.Rest.Annotations
{
    [AttributeUsage(AttributeTargets.Method|AttributeTargets.Property)]
    public class GetAttribute : VerbAttribute
    {
        public GetAttribute():base("GET")
        {
        }

        public GetAttribute(string route) : base("GET",route)
        {
        }
    }
}