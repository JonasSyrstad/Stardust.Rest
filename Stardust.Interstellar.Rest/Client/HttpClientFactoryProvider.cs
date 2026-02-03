using System;
using System.Net.Http;

namespace Stardust.Interstellar.Rest.Client
{
    /// <summary>
    /// HttpClient provider that uses IHttpClientFactory for proper connection management.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This provider leverages IHttpClientFactory which provides:
    /// </para>
    /// <list type="bullet">
    /// <item><description>Proper connection pooling with configurable lifetime</description></item>
    /// <item><description>DNS change handling through connection recycling</description></item>
    /// <item><description>Automatic disposal of expired connections</description></item>
    /// <item><description>Integration with Polly for resilience policies</description></item>
    /// </list>
    /// <para>
    /// <strong>Recommended for production use.</strong>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // In Startup.cs or Program.cs
    /// services.AddHttpClient("StardustRest", client =>
    /// {
    ///     client.Timeout = TimeSpan.FromSeconds(100);
    /// })
    /// .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
    /// {
    ///     PooledConnectionLifetime = TimeSpan.FromMinutes(15),
    ///     MaxConnectionsPerServer = 100
    /// });
    /// 
    /// services.AddSingleton&lt;IHttpClientProvider, HttpClientFactoryProvider&gt;();
    /// </code>
    /// </example>
    public class HttpClientFactoryProvider : IHttpClientProvider
    {
        /// <summary>
        /// The default name used when creating HttpClient instances from the factory.
        /// </summary>
        public const string DefaultClientName = "StardustRest";

        private readonly System.Net.Http.IHttpClientFactory _httpClientFactory;
        private readonly string _clientName;

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpClientFactoryProvider"/> class.
        /// </summary>
        /// <param name="httpClientFactory">The HTTP client factory.</param>
        public HttpClientFactoryProvider(System.Net.Http.IHttpClientFactory httpClientFactory)
            : this(httpClientFactory, DefaultClientName)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpClientFactoryProvider"/> class.
        /// </summary>
        /// <param name="httpClientFactory">The HTTP client factory.</param>
        /// <param name="clientName">The named client configuration to use.</param>
        public HttpClientFactoryProvider(System.Net.Http.IHttpClientFactory httpClientFactory, string clientName)
        {
            _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
            _clientName = clientName ?? DefaultClientName;
        }

        /// <inheritdoc />
        /// <remarks>
        /// Note: When using IHttpClientFactory, the BaseAddress is not set on the client.
        /// The full URL is constructed by the RestWrapper for each request.
        /// Timeout is configured through the named client configuration in DI.
        /// </remarks>
        public HttpClient GetClient(string baseUri)
        {
            return _httpClientFactory.CreateClient(_clientName);
        }

        /// <inheritdoc />
        /// <remarks>
        /// Note: When using IHttpClientFactory, the timeout should be configured through 
        /// the named client configuration in DI. This method ignores the timeout parameter
        /// as the factory-created client's timeout is managed by the factory configuration.
        /// </remarks>
        public HttpClient GetClient(string baseUri, TimeSpan timeout)
        {
            // IHttpClientFactory manages client configuration including timeout
            // The timeout should be configured in the named client registration
            return _httpClientFactory.CreateClient(_clientName);
        }
    }
}
