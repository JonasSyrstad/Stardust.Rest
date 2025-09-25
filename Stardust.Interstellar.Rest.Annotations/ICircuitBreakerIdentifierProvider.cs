using System;

namespace Stardust.Interstellar.Rest.Annotations
{
    /// <summary>
    /// Interface for extracting user and client identifiers for scoped circuit breakers
    /// </summary>
    public interface ICircuitBreakerIdentifierProvider
    {
        /// <summary>
        /// Gets the current user identifier for circuit breaker scoping
        /// </summary>
        /// <param name="serviceProvider">Service provider for accessing context services</param>
        /// <param name="userIdentifierProperty">Optional property name/claim type to look for</param>
        /// <returns>User identifier or null if not available</returns>
        string GetUserIdentifier(IServiceProvider serviceProvider, string userIdentifierProperty = null);

        /// <summary>
        /// Gets the current client identifier for circuit breaker scoping
        /// </summary>
        /// <param name="serviceProvider">Service provider for accessing context services</param>
        /// <param name="clientIdentifierProperty">Optional property name/claim type to look for</param>
        /// <returns>Client identifier or null if not available</returns>
        string GetClientIdentifier(IServiceProvider serviceProvider, string clientIdentifierProperty = null);
    }
}