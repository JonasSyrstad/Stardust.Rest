using System;

namespace Stardust.Interstellar.Rest.Annotations
{
	/// <summary>
	/// Tells the code generation tools to create a webapi controller for this interface
	/// </summary>
	[AttributeUsage(AttributeTargets.Interface)]
	public class ApiAttribute : ServiceDescriptionAttribute, IRoutePrefix
	{
		private readonly string prefix;

		private readonly bool includeTypeName;

		public ApiAttribute(string prefix):base("")
		{
			this.prefix = prefix;
		}

		public ApiAttribute(string prefix,string serviceDescription) : base(serviceDescription)
		{
			this.prefix = prefix;
		}

		public ApiAttribute(string prefix, bool includeTypeName) : base("")
		{
			this.prefix = prefix;
			this.includeTypeName = includeTypeName;
		}

		public ApiAttribute(string prefix, bool includeTypeName, string serviceDescription) : base(serviceDescription)
		{
			this.prefix = prefix;
			this.includeTypeName = includeTypeName;
		}

		public string Prefix
		{
			get
			{
				return prefix;
			}
		}

		public bool IncludeTypeName
		{
			get
			{
				return includeTypeName;
			}
		}

	}
}