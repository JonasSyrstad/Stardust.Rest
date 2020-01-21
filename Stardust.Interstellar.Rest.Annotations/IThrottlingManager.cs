namespace Stardust.Interstellar.Rest.Annotations
{
	public interface IThrottlingManager
	{
		/// <summary>
		/// Determines whether the specified method is throttled. If throttled, it returns the wait time in ms, if not null
		/// </summary>
		/// <param name="method">The method.</param>
		/// <param name="service">The service.</param>
		/// <param name="host">The host.</param>
		/// <returns></returns>
		long? IsThrottled(string method, string service, string host);
	}
}