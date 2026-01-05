using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading.Tasks;
#if NET6_0_OR_GREATER
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
#endif

namespace Stardust.Interstellar.Rest.Annotations.EndpointRouting
{
    /// <summary>
    /// Only applicable for .net6 +
    /// </summary>
    [AttributeUsage(AttributeTargets.Interface | AttributeTargets.Method, Inherited = false, AllowMultiple = true)]
    public class EndpointRoutingPolicyAttribute : Attribute
    {
        public string PolicyName { get; set; }
        public EndpointRoutingPolicyAttribute() { }

        public EndpointRoutingPolicyAttribute(string policyName)
        {
            PolicyName = policyName;
        }
    }

#if NET6_0_OR_GREATER
    /// <summary>
    /// Interface for applying custom routing policies to minimal API endpoints.
    /// Implementations can customize Swagger documentation, response headers, and other endpoint metadata.
    /// </summary>
    public interface IEndpointRoutingPolicy
    {
        /// <summary>
        /// The unique name of this policy, used to match against EndpointRoutingPolicyAttribute.PolicyName
        /// </summary>
        string PolicyName { get; }

        /// <summary>
        /// Applies the policy to the route handler builder.
        /// Use this method to add authorization, rate limiting, caching, or other endpoint configurations.
        /// </summary>
        /// <param name="routeBuilder">The route handler builder to configure</param>
        /// <returns>The configured route handler builder for chaining</returns>
        RouteHandlerBuilder ApplyPolicyAsync(RouteHandlerBuilder routeBuilder);

        /// <summary>
        /// Gets the OpenAPI/Swagger customization settings for this policy.
        /// Return null to skip OpenAPI customization.
        /// </summary>
        /// <returns>OpenAPI customization settings or null</returns>
        OpenApiPolicySettings GetOpenApiSettings() => null;

        /// <summary>
        /// Gets the response headers to add to all responses from endpoints using this policy.
        /// Return null or empty dictionary to skip adding headers.
        /// </summary>
        /// <returns>Dictionary of header names and values, or null</returns>
        IDictionary<string, string> GetResponseHeaders() => null;

        /// <summary>
        /// Called during request processing to allow dynamic response header modification.
        /// </summary>
        /// <param name="context">The HTTP context for the current request</param>
        /// <param name="headers">The response headers dictionary to modify</param>
        void OnBeforeResponse(HttpContext context, IDictionary<string, string> headers) { }
    }

    /// <summary>
    /// Settings for customizing OpenAPI/Swagger documentation for an endpoint.
    /// </summary>
    public class OpenApiPolicySettings
    {
        /// <summary>
        /// Gets or sets the summary for the endpoint in OpenAPI documentation.
        /// </summary>
        public string Summary { get; set; }

        /// <summary>
        /// Gets or sets the detailed description for the endpoint in OpenAPI documentation.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the tags for grouping endpoints in OpenAPI documentation.
        /// </summary>
        public string[] Tags { get; set; }

        /// <summary>
        /// Gets or sets whether the endpoint should be marked as deprecated.
        /// </summary>
        public bool IsDeprecated { get; set; }

        /// <summary>
        /// Gets or sets the deprecation message if IsDeprecated is true.
        /// </summary>
        public string DeprecationMessage { get; set; }

        /// <summary>
        /// Gets or sets additional response types to document in OpenAPI.
        /// Key is status code, Value is the response type and description.
        /// </summary>
        public IDictionary<int, OpenApiResponseInfo> AdditionalResponses { get; set; }

        /// <summary>
        /// Gets or sets custom metadata objects to attach to the endpoint.
        /// </summary>
        public object[] CustomMetadata { get; set; }

        /// <summary>
        /// Gets or sets the operation ID for OpenAPI documentation.
        /// </summary>
        public string OperationId { get; set; }

        /// <summary>
        /// Gets or sets additional content types the endpoint accepts.
        /// </summary>
        public string[] AdditionalContentTypes { get; set; }
    }

    /// <summary>
    /// Information about a response type for OpenAPI documentation.
    /// </summary>
    public class OpenApiResponseInfo
    {
        /// <summary>
        /// Gets or sets the response type. Use null for responses without a body.
        /// </summary>
        public Type ResponseType { get; set; }

        /// <summary>
        /// Gets or sets the description of this response.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the content type for this response. Defaults to "application/json".
        /// </summary>
        public string ContentType { get; set; } = "application/json";

        /// <summary>
        /// Creates a new OpenApiResponseInfo instance.
        /// </summary>
        public OpenApiResponseInfo() { }

        /// <summary>
        /// Creates a new OpenApiResponseInfo instance with the specified type and description.
        /// </summary>
        /// <param name="responseType">The response body type</param>
        /// <param name="description">The response description</param>
        public OpenApiResponseInfo(Type responseType, string description)
        {
            ResponseType = responseType;
            Description = description;
        }

        /// <summary>
        /// Creates a response info for a status-code-only response (no body).
        /// </summary>
        /// <param name="description">The response description</param>
        /// <returns>A new OpenApiResponseInfo instance</returns>
        public static OpenApiResponseInfo NoBody(string description) => new OpenApiResponseInfo { Description = description };
    }

    /// <summary>
    /// Base class for implementing endpoint routing policies with common functionality.
    /// </summary>
    public abstract class EndpointRoutingPolicyBase : IEndpointRoutingPolicy
    {
        /// <inheritdoc />
        public abstract string PolicyName { get; }

        /// <inheritdoc />
        public virtual RouteHandlerBuilder ApplyPolicyAsync(RouteHandlerBuilder routeBuilder)
        {
            return routeBuilder;
        }

        /// <inheritdoc />
        public virtual OpenApiPolicySettings GetOpenApiSettings() => null;

        /// <inheritdoc />
        public virtual IDictionary<string, string> GetResponseHeaders() => null;

        /// <inheritdoc />
        public virtual void OnBeforeResponse(HttpContext context, IDictionary<string, string> headers) { }
    }
#endif
}
