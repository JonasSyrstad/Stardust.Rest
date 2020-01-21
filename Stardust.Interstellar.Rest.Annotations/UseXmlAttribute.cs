using System;

namespace Stardust.Interstellar.Rest.Annotations
{
	/// <summary>
	/// Causes the generated client to use xml as messaging format.
	/// </summary>
	[AttributeUsage(AttributeTargets.Interface|AttributeTargets.Method, AllowMultiple = true)]
	public class UseXmlAttribute : Attribute
	{
        
	}
}