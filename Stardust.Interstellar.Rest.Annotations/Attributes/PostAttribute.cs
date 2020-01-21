using System;

namespace Stardust.Interstellar.Rest.Annotations
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Property)]
    public class PostAttribute : VerbAttribute
    {
        public PostAttribute() : base("POST")
        {
        }

        public PostAttribute(string route) : base("POST",route)
        {
        }

	    public PostAttribute(string route, string serviceDescription) : base("POST", route, serviceDescription)
	    {
	    }
	}
}