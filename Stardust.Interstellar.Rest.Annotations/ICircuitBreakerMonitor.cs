using System;

namespace Stardust.Interstellar.Rest.Annotations
{
	public interface ICircuitBreakerMonitor
	{
		/// <summary>
		/// The Circuit Breaker is opened. Add notifications and proactive efforts here
		/// </summary>
		/// <param name="circuitBreakerServiceName"></param>
		/// <param name="exception"></param>
		/// <param name="state"></param>
		void Trip(string circuitBreakerServiceName, Exception exception, ICircuitBreakerState state, IServiceProvider provider);

		/// <summary>
		/// Programatically determine if an exception is a part of the application flow of control or if it is broken and should be suspended
		/// </summary>
		/// <param name="exception"></param>
		/// <returns></returns>
		bool IsExceptionIgnorable(Exception exception);
	}
}