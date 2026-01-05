using System;

namespace Stardust.Interstellar.Rest.Annotations
{
	[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
	public class InQueryAttribute : InAttribute
	{
		/// <summary>
		/// Marks a parameter to be included in the query string using the C# parameter name.
		/// </summary>
		public InQueryAttribute() : base(InclutionTypes.Query)
		{
		}

		/// <summary>
		/// Marks a parameter to be included in the query string using a custom name.
		/// </summary>
		/// <param name="name">The custom name to use for the query string parameter.</param>
		public InQueryAttribute(string name) : base(InclutionTypes.Query)
		{
			Name = name;
		}
	}
}