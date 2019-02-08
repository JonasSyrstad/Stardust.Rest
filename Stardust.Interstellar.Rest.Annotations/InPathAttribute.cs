using System;

namespace Stardust.Interstellar.Rest.Annotations
{
	[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
	public class InPathAttribute : InAttribute
	{
		public InPathAttribute():base(InclutionTypes.Path)
		{
            
		}
	}
}