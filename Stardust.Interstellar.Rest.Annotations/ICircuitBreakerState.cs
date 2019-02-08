namespace Stardust.Interstellar.Rest.Annotations
{
	public interface ICircuitBreakerState
	{
		ICircuit Circuit { get; }

		string State { get; }
	}
}