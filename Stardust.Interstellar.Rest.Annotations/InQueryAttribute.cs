using System;

namespace Stardust.Interstellar.Rest.Annotations
{
	[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
	public class InQueryAttribute : InAttribute
	{
		public InQueryAttribute() : base(InclutionTypes.Query)
		{

		}
	}
}