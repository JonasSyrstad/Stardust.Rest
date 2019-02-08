using System;

namespace Stardust.Interstellar.Rest.Annotations
{
	[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
	public class InHeaderAttribute : InAttribute
	{
		public InHeaderAttribute() : base(InclutionTypes.Header)
		{

		}
	}
}