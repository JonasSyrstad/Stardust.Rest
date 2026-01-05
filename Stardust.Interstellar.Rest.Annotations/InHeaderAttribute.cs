using System;

namespace Stardust.Interstellar.Rest.Annotations
{
	[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
	public class InHeaderAttribute : InAttribute
	{
		/// <summary>
		/// Marks a parameter to be included in the request headers using the C# parameter name.
		/// </summary>
		public InHeaderAttribute() : base(InclutionTypes.Header)
		{
		}

		/// <summary>
		/// Marks a parameter to be included in the request headers using a custom name.
		/// </summary>
		/// <param name="name">The custom name to use for the header.</param>
		public InHeaderAttribute(string name) : base(InclutionTypes.Header)
		{
			Name = name;
		}
	}
}