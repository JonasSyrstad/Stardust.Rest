using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using Microsoft.Extensions.Primitives;
using Stardust.Interstellar.Rest.Extensions;
using Stardust.Particles.Collection;

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

	    public DeleteAttribute(string route,string serviceDescription) : base("DELETE", route,serviceDescription)
	    {
	    }
	}
}