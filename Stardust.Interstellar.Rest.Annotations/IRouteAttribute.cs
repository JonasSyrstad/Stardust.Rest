using System;

namespace Stardust.Interstellar.Rest.Annotations
{
	[Obsolete("Put the route information in the Verb attribute",false)]
	public class IRouteAttribute : Attribute,IRoute
	{
		public IRouteAttribute(string template)
		{
			Template = template;
		}

		public string Template { get; set; }
	}
}