using System;

namespace Stardust.Interstellar.Rest.Annotations
{
    [AttributeUsage(AttributeTargets.Method )]
    public class PutAttribute : VerbAttribute
    {
        public PutAttribute() : base("PUT")
        {
        }

        public PutAttribute(string route) : base("PUT",route)
        {
        }

	    public PutAttribute(string route, string serviceDescription) : base("PUT", route, serviceDescription)
	    {
	    }
	}
}