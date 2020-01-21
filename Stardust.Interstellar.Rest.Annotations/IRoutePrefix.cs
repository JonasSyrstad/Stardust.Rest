using System;

namespace Stardust.Interstellar.Rest.Annotations
{
	public interface IRoutePrefix
	{
		string Prefix { get; }
		[Obsolete]
		bool IncludeTypeName { get; }
	}
}