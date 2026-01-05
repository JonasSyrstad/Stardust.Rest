using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Stardust.Interstellar.Rest.Annotations.EndpointRouting;

namespace NetCore3TestApp.Api
{
    /// <summary>
    /// Sample caching policy that adds cache headers to responses.
    /// </summary>
    public class CachingPolicy : EndpointRoutingPolicyBase
    {
        private readonly int _maxAgeSeconds;

        public CachingPolicy(int maxAgeSeconds = 300)
        {
            _maxAgeSeconds = maxAgeSeconds;
        }

        public override string PolicyName => "Caching";

        public override RouteHandlerBuilder ApplyPolicyAsync(RouteHandlerBuilder routeBuilder)
        {
            // Add caching metadata for OpenAPI
            return routeBuilder;
        }

        public override OpenApiPolicySettings GetOpenApiSettings()
        {
            return new OpenApiPolicySettings
            {
                Tags = new[] { "Cached" },
                AdditionalResponses = new Dictionary<int, OpenApiResponseInfo>
                {
                    { 304, OpenApiResponseInfo.NoBody("Not Modified - cached response is still valid") }
                }
            };
        }

        public override IDictionary<string, string> GetResponseHeaders()
        {
            return new Dictionary<string, string>
            {
                { "Cache-Control", $"public, max-age={_maxAgeSeconds}" },
                { "Vary", "Accept, Accept-Encoding" }
            };
        }

        public override void OnBeforeResponse(HttpContext context, IDictionary<string, string> headers)
        {
            // Add ETag based on request path for simple cache validation
            var etag = $"\"{context.Request.Path.GetHashCode():X}\"";
            headers["ETag"] = etag;
        }
    }

    /// <summary>
    /// Sample rate limiting policy that adds rate limit headers.
    /// </summary>
    public class RateLimitingPolicy : EndpointRoutingPolicyBase
    {
        private readonly int _requestsPerMinute;

        public RateLimitingPolicy(int requestsPerMinute = 60)
        {
            _requestsPerMinute = requestsPerMinute;
        }

        public override string PolicyName => "RateLimiting";

        public override OpenApiPolicySettings GetOpenApiSettings()
        {
            return new OpenApiPolicySettings
            {
                Description = $"This endpoint is rate limited to {_requestsPerMinute} requests per minute.",
                AdditionalResponses = new Dictionary<int, OpenApiResponseInfo>
                {
                    { 429, OpenApiResponseInfo.NoBody("Too Many Requests - rate limit exceeded") }
                }
            };
        }

        public override IDictionary<string, string> GetResponseHeaders()
        {
            return new Dictionary<string, string>
            {
                { "X-RateLimit-Limit", _requestsPerMinute.ToString() },
                { "X-RateLimit-Policy", "sliding-window" }
            };
        }

        public override void OnBeforeResponse(HttpContext context, IDictionary<string, string> headers)
        {
            // In a real implementation, you would track actual usage
            // This is just a demonstration
            headers["X-RateLimit-Remaining"] = (_requestsPerMinute - 1).ToString();
            headers["X-RateLimit-Reset"] = DateTimeOffset.UtcNow.AddMinutes(1).ToUnixTimeSeconds().ToString();
        }
    }

    /// <summary>
    /// Sample CORS policy that adds CORS-related headers.
    /// </summary>
    public class ApiCorsPolicy : EndpointRoutingPolicyBase
    {
        private readonly string _allowedOrigins;

        public ApiCorsPolicy(string allowedOrigins = "*")
        {
            _allowedOrigins = allowedOrigins;
        }

        public override string PolicyName => "ApiCors";

        public override IDictionary<string, string> GetResponseHeaders()
        {
            return new Dictionary<string, string>
            {
                { "Access-Control-Allow-Origin", _allowedOrigins },
                { "Access-Control-Allow-Methods", "GET, POST, PUT, DELETE, PATCH" },
                { "Access-Control-Allow-Headers", "Content-Type, Authorization" }
            };
        }
    }

    /// <summary>
    /// Sample security headers policy.
    /// </summary>
    public class SecurityHeadersPolicy : EndpointRoutingPolicyBase
    {
        public override string PolicyName => "SecurityHeaders";

        public override IDictionary<string, string> GetResponseHeaders()
        {
            return new Dictionary<string, string>
            {
                { "X-Content-Type-Options", "nosniff" },
                { "X-Frame-Options", "DENY" },
                { "X-XSS-Protection", "1; mode=block" },
                { "Referrer-Policy", "strict-origin-when-cross-origin" }
            };
        }
    }

    /// <summary>
    /// Sample API versioning policy that adds version headers.
    /// </summary>
    public class ApiVersionPolicy : EndpointRoutingPolicyBase
    {
        private readonly string _version;
        private readonly string _deprecationDate;

        public ApiVersionPolicy(string version = "1.0", string? deprecationDate = null)
        {
            _version = version;
            _deprecationDate = deprecationDate;
        }

        public override string PolicyName => "ApiVersion";

        public override OpenApiPolicySettings GetOpenApiSettings()
        {
            var settings = new OpenApiPolicySettings
            {
                Tags = new[] { $"v{_version}" }
            };

            if (!string.IsNullOrEmpty(_deprecationDate))
            {
                settings.IsDeprecated = true;
                settings.DeprecationMessage = $"This API version will be deprecated on {_deprecationDate}. Please migrate to the latest version.";
            }

            return settings;
        }

        public override IDictionary<string, string> GetResponseHeaders()
        {
            var headers = new Dictionary<string, string>
            {
                { "X-API-Version", _version }
            };

            if (!string.IsNullOrEmpty(_deprecationDate))
            {
                headers["X-API-Deprecation-Date"] = _deprecationDate;
                headers["Sunset"] = _deprecationDate;
            }

            return headers;
        }
    }
}
