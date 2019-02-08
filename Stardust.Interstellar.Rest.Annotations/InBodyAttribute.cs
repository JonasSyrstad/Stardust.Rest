using System;

namespace Stardust.Interstellar.Rest.Annotations
{
	[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
	public class InBodyAttribute : InAttribute
	{
		public InBodyAttribute() : base(InclutionTypes.Body)
		{

		}
	}
}