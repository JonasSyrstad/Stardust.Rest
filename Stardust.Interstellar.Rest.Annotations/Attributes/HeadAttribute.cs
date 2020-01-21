using System;

namespace Stardust.Interstellar.Rest.Annotations
{
    [AttributeUsage(AttributeTargets.Method)]
    public class HeadAttribute : VerbAttribute
    {
        public HeadAttribute() : base("HEAD")
        {
        }

        public HeadAttribute(string route) : base("HEAD",route)
        {
        }

	    public HeadAttribute(string route, string serviceDescription) : base("HEAD", route, serviceDescription)
	    {
	    }
	}
}