using System;
using System.ComponentModel;

namespace Stardust.Interstellar.Rest.Annotations
{
    /// <summary>
    /// Marks a method parameter to be included in the HTTP request headers.
    /// The parameter value will be sent as an HTTP header.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Use <see cref="InHeaderAttribute"/> when you need to pass metadata, authentication tokens, 
    /// request identifiers, or other cross-cutting concerns that don't belong in the URL or body.
    /// </para>
    /// <para>
    /// <strong>When to use:</strong>
    /// </para>
    /// <list type="bullet">
    /// <item><description>Authentication and authorization tokens (e.g., API keys, Bearer tokens)</description></item>
    /// <item><description>Request tracking identifiers (e.g., correlation IDs, request IDs)</description></item>
    /// <item><description>Content negotiation (e.g., Accept-Language, Accept-Encoding)</description></item>
    /// <item><description>Client version information</description></item>
    /// <item><description>Custom metadata that applies to the entire request</description></item>
    /// </list>
    /// <para>
    /// <strong>Custom header names:</strong>
    /// You can specify a custom name for the HTTP header using the constructor or the <see cref="Name"/> property.
    /// This is essential when working with standard HTTP headers or custom headers that follow specific naming conventions.
    /// </para>
    /// <para>
    /// <strong>Best practices:</strong>
    /// </para>
    /// <list type="bullet">
    /// <item><description>Use standard HTTP header names when applicable (e.g., "Authorization", "Accept-Language")</description></item>
    /// <item><description>Prefix custom headers with "X-" for non-standard headers (e.g., "X-Api-Key", "X-Request-Id")</description></item>
    /// <item><description>Keep header names consistent across your API</description></item>
    /// </list>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Basic usage with C# parameter names
    /// [Api("api/users")]
    /// public interface IUserService
    /// {
    ///     [Get("{id}")]
    ///     Task&lt;User&gt; GetUser(
    ///         [InPath] string id,
    ///         [InHeader] string authorization);
    ///     // Sends header: authorization: Bearer token123
    /// }
    /// 
    /// // Using standard HTTP header names
    /// [Api("api/content")]
    /// public interface IContentService
    /// {
    ///     [Get("data")]
    ///     Task&lt;Content&gt; GetContent(
    ///         [InHeader("Authorization")] string authToken,
    ///         [InHeader("Accept-Language")] string language,
    ///         [InHeader("If-None-Match")] string etag);
    ///     // Sends headers with proper HTTP names:
    ///     // Authorization: Bearer token123
    ///     // Accept-Language: en-US
    ///     // If-None-Match: "abc123"
    /// }
    /// 
    /// // Using custom header names for API keys and tracking
    /// [Api("api/data")]
    /// public interface IDataService
    /// {
    ///     [Get("items")]
    ///     Task&lt;List&lt;Item&gt;&gt; GetItems(
    ///         [InHeader("X-Api-Key")] string apiKey,
    ///         [InHeader("X-Request-Id")] string requestId,
    ///         [InHeader("X-Client-Version")] string clientVersion);
    ///     // Sends headers:
    ///     // X-Api-Key: abc123def456
    ///     // X-Request-Id: guid-1234
    ///     // X-Client-Version: 2.0.1
    /// }
    /// </code>
    /// </example>
	[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
	public class InHeaderAttribute : InAttribute
	{
		/// <summary>
		/// Marks a parameter to be included in the request headers using the C# parameter name.
		/// </summary>
		public InHeaderAttribute() : base(InclutionTypes.Header)
		{
		}

		/// <summary>
		/// Marks a parameter to be included in the request headers using a custom name or with a description.
		/// </summary>
		/// <param name="name">The custom name for the header. Use null or empty to use the C# parameter name.</param>
        /// <param name="Description">Optional description for the parameter to be used in generated documentation.</param>
        /// <example>
        /// <code>
        /// // Parameter 'apiKey' becomes 'X-Api-Key' header with a description
        /// Task&lt;Data&gt; GetData(
        ///     [InHeader("X-Api-Key", Description = "API key for authentication")] string apiKey);
        /// // Sends header: X-Api-Key: abc123
        /// 
        /// // Parameter uses C# name with only description
        /// Task&lt;Data&gt; GetData(
        ///     [InHeader(Description = "API key for authentication")] string apiKey);
        /// </code>
        /// </example>
		public InHeaderAttribute(string name = null) : base(InclutionTypes.Header, name)
		{
		}

        /// <summary>
        /// Gets or sets the custom name to use for the HTTP header.
        /// When set, this name will be used instead of the C# parameter name.
        /// </summary>
        /// <value>
        /// The custom header name, or <c>null</c> to use the C# parameter name.
        /// </value>
        /// <remarks>
        /// Use this property to specify standard HTTP header names (e.g., "Authorization", "Content-Type")
        /// or custom headers following your API conventions (e.g., "X-Api-Key", "X-Request-Id").
        /// </remarks>
        [EditorBrowsable(EditorBrowsableState.Always)]
        public override string  Name { get; set; }
	}
}