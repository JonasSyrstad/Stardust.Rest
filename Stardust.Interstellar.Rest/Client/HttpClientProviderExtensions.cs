using System;
using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Stardust.Interstellar.Rest.Client
{
    /// <summary>
    /// Extension methods for configuring HttpClient providers in dependency injection.
    /// </summary>
    public static class HttpClientProviderExtensions
    {
        /// <summary>
        /// Adds the default HttpClient provider to the service collection.
        /// </summary>
        /// <remarks>
        /// The default provider uses a static dictionary to cache HttpClient instances per base URL.
        /// This prevents socket exhaustion but does not handle DNS changes.
        /// For production workloads, consider using <see cref="AddStardustHttpClientFactory"/> instead.
        /// </remarks>
        /// <param name="services">The service collection.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddStardustDefaultHttpClientProvider(this IServiceCollection services)
        {
            services.AddSingleton<IHttpClientProvider, DefaultHttpClientProvider>();
            return services;
        }

        /// <summary>
        /// Adds HttpClientFactory-based HttpClient provider to the service collection.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This is the recommended approach for production workloads as it provides:
        /// </para>
        /// <list type="bullet">
        /// <item><description>Proper connection pooling with configurable lifetime</description></item>
        /// <item><description>DNS change handling through connection recycling</description></item>
        /// <item><description>Automatic disposal of expired connections</description></item>
        /// </list>
        /// </remarks>
        /// <param name="services">The service collection.</param>
        /// <returns>The IHttpClientBuilder for further configuration.</returns>
        /// <example>
        /// <code>
        /// services.AddStardustHttpClientFactory()
        ///     .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
        ///     {
        ///         PooledConnectionLifetime = TimeSpan.FromMinutes(15),
        ///         MaxConnectionsPerServer = 100
        ///     });
        /// </code>
        /// </example>
        public static Microsoft.Extensions.DependencyInjection.IHttpClientBuilder AddStardustHttpClientFactory(this IServiceCollection services)
        {
            return AddStardustHttpClientFactory(services, null);
        }

        /// <summary>
        /// Adds HttpClientFactory-based HttpClient provider to the service collection with custom configuration.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configureClient">An action to configure the HttpClient.</param>
        /// <returns>The IHttpClientBuilder for further configuration.</returns>
        /// <example>
        /// <code>
        /// services.AddStardustHttpClientFactory(client =>
        /// {
        ///     client.Timeout = TimeSpan.FromSeconds(30);
        ///     client.DefaultRequestHeaders.Add("User-Agent", "MyApp/1.0");
        /// })
        /// .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
        /// {
        ///     PooledConnectionLifetime = TimeSpan.FromMinutes(15)
        /// });
        /// </code>
        /// </example>
        public static Microsoft.Extensions.DependencyInjection.IHttpClientBuilder AddStardustHttpClientFactory(
            this IServiceCollection services,
            Action<HttpClient> configureClient)
        {
            services.AddSingleton<IHttpClientProvider, HttpClientFactoryProvider>();

            var builder = services.AddHttpClient(HttpClientFactoryProvider.DefaultClientName);
            
            if (configureClient != null)
            {
                builder.ConfigureHttpClient(configureClient);
            }
            else
            {
                // Apply default configuration
                builder.ConfigureHttpClient(client =>
                {
                    client.Timeout = TimeSpan.FromSeconds(ClientGlobalSettings.Timeout ?? 100);
                });
            }

            return builder;
        }

        /// <summary>
        /// Adds HttpClientFactory-based HttpClient provider with connection lifetime management for DNS changes.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="pooledConnectionLifetime">
        /// The maximum lifetime of connections in the pool. 
        /// When this time elapses, the connection is recycled, allowing DNS changes to take effect.
        /// Default is 15 minutes if not specified.
        /// </param>
        /// <returns>The IHttpClientBuilder for further configuration.</returns>
        /// <example>
        /// <code>
        /// // Configure with 5-minute connection lifetime for faster DNS refresh
        /// services.AddStardustHttpClientFactoryWithLifetime(TimeSpan.FromMinutes(5));
        /// </code>
        /// </example>
        public static Microsoft.Extensions.DependencyInjection.IHttpClientBuilder AddStardustHttpClientFactoryWithLifetime(
            this IServiceCollection services,
            TimeSpan? pooledConnectionLifetime = null)
        {
            var lifetime = pooledConnectionLifetime ?? TimeSpan.FromMinutes(15);

            return services.AddStardustHttpClientFactory()
                .SetHandlerLifetime(lifetime);
        }
    }
}
