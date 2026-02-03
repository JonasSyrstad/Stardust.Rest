using System;
using System.Collections.Concurrent;
using System.Net.Http;

namespace Stardust.Interstellar.Rest.Client
{
    /// <summary>
    /// Default HttpClient provider that maintains a static dictionary of HttpClient instances.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This provider reuses HttpClient instances to avoid socket exhaustion.
    /// It uses <see cref="ConcurrentDictionary{TKey,TValue}.GetOrAdd"/> to prevent race conditions
    /// when multiple threads request clients for the same base URI simultaneously.
    /// </para>
    /// <para>
    /// <strong>Limitations:</strong>
    /// </para>
    /// <list type="bullet">
    /// <item><description>Does not handle DNS changes - static clients won't respect DNS TTL</description></item>
    /// <item><description>Clients are never removed from memory</description></item>
    /// </list>
    /// <para>
    /// For production workloads, consider using <see cref="HttpClientFactoryProvider"/> with 
    /// IHttpClientFactory for proper connection lifetime management.
    /// </para>
    /// </remarks>
    public class DefaultHttpClientProvider : IHttpClientProvider
    {
        private static readonly ConcurrentDictionary<string, HttpClient> _clients = 
            new ConcurrentDictionary<string, HttpClient>(StringComparer.OrdinalIgnoreCase);

        /// <inheritdoc />
        public HttpClient GetClient(string baseUri)
        {
            return GetClient(baseUri, TimeSpan.FromSeconds(ClientGlobalSettings.Timeout ?? 100));
        }

        /// <inheritdoc />
        public HttpClient GetClient(string baseUri, TimeSpan timeout)
        {
            if (string.IsNullOrWhiteSpace(baseUri))
                throw new ArgumentNullException(nameof(baseUri));

            var key = baseUri.ToLowerInvariant();
            
            // Use GetOrAdd to prevent race condition - factory is only called once per key
            return _clients.GetOrAdd(key, uri => CreateClient(uri, timeout));
        }

        private static HttpClient CreateClient(string baseUri, TimeSpan timeout)
        {
            var client = new HttpClient
            {
                BaseAddress = new Uri(baseUri),
                Timeout = timeout
            };

            return client;
        }

        /// <summary>
        /// Clears all cached HttpClient instances.
        /// </summary>
        /// <remarks>
        /// <strong>Warning:</strong> This will dispose all cached clients. 
        /// Any in-flight requests using these clients may fail.
        /// Use with caution and only during application shutdown or reconfiguration.
        /// </remarks>
        public static void ClearClients()
        {
            foreach (var client in _clients.Values)
            {
                try
                {
                    client.Dispose();
                }
                catch
                {
                    // Ignore disposal errors
                }
            }
            _clients.Clear();
        }

        /// <summary>
        /// Gets the number of cached HttpClient instances.
        /// </summary>
        public static int CachedClientCount => _clients.Count;
    }
}
