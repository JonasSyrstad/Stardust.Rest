using System;

namespace Stardust.Interstellar.Rest.Annotations
{
	public interface ICircuit
	{
		int Failures { get; }

		DateTime? LastFailure { get; }

		TimeSpan? SuspendedTime { get; }

		Exception LastError { get; }
	}
}