using System;
using System.Net.Http;

namespace Stardust.Interstellar.Rest.Client
{
    /// <summary>
    /// Provides HttpClient instances for REST client operations.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This interface allows for different HttpClient management strategies:
    /// </para>
    /// <list type="bullet">
    /// <item><description>Default: Static dictionary-based client reuse (backward compatible)</description></item>
    /// <item><description>IHttpClientFactory: Proper connection pooling with DNS refresh support</description></item>
    /// <item><description>Custom: Any user-defined strategy</description></item>
    /// </list>
    /// <para>
    /// <strong>Recommended:</strong> Use IHttpClientFactory integration in production for proper
    /// connection lifetime management and DNS change handling.
    /// </para>
    /// </remarks>
    public interface IHttpClientProvider
    {
        /// <summary>
        /// Gets an HttpClient instance for the specified base URI.
        /// </summary>
        /// <param name="baseUri">The base URI for the HTTP client.</param>
        /// <returns>An HttpClient configured for the specified base URI.</returns>
        HttpClient GetClient(string baseUri);

        /// <summary>
        /// Gets an HttpClient instance for the specified base URI with a custom timeout.
        /// </summary>
        /// <param name="baseUri">The base URI for the HTTP client.</param>
        /// <param name="timeout">The timeout for HTTP operations.</param>
        /// <returns>An HttpClient configured for the specified base URI and timeout.</returns>
        HttpClient GetClient(string baseUri, TimeSpan timeout);
    }
}
