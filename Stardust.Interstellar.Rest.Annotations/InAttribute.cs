using System;
using System.ComponentModel;
using Stardust.Particles;

namespace Stardust.Interstellar.Rest.Annotations
{
    /// <summary>
    /// Base attribute for specifying how method parameters should be included in HTTP requests/responses.
    /// This attribute controls where a parameter's value is placed in the HTTP message.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The <see cref="InAttribute"/> is the base class for a family of attributes that determine how method parameters
    /// are mapped to different parts of an HTTP request (path, query string, headers, or body).
    /// </para>
    /// <para>
    /// <strong>When to use:</strong>
    /// </para>
    /// <list type="bullet">
    /// <item><description>Apply to method parameters in REST service interface definitions</description></item>
    /// <item><description>Use derived attributes (<see cref="InPathAttribute"/>, <see cref="InQueryAttribute"/>, <see cref="InHeaderAttribute"/>, <see cref="InBodyAttribute"/>) for specific parameter placement</description></item>
    /// <item><description>Required for all parameters in service interface methods to explicitly define their HTTP location</description></item>
    /// </list>
    /// <para>
    /// <strong>Available derived attributes:</strong>
    /// </para>
    /// <list type="bullet">
    /// <item><description><see cref="InPathAttribute"/> - Parameter is part of the URL path (e.g., /api/users/{id})</description></item>
    /// <item><description><see cref="InQueryAttribute"/> - Parameter is included in the query string (e.g., ?name=value)</description></item>
    /// <item><description><see cref="InHeaderAttribute"/> - Parameter is sent as an HTTP header</description></item>
    /// <item><description><see cref="InBodyAttribute"/> - Parameter is serialized in the request body (typically for POST/PUT)</description></item>
    /// </list>
    /// <para>
    /// <strong>Default behavior:</strong> If no derived attribute is specified, the parameter defaults to <see cref="InclusionTypes.Path"/>.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// [Api("api/users")]
    /// public interface IUserService
    /// {
    ///     [Get("{id}")]
    ///     Task&lt;User&gt; GetUser([InPath] string id);
    ///     
    ///     [Get("search")]
    ///     Task&lt;List&lt;User&gt;&gt; SearchUsers([InQuery] string name, [InQuery] int pageSize);
    ///     
    ///     [Post("")]
    ///     Task&lt;User&gt; CreateUser([InBody] User user, [InHeader] string authToken);
    /// }
    /// </code>
    /// </example>
    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
    public class InAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="InAttribute"/> class with the specified inclusion type.
        /// </summary>
        /// <param name="InclusionType">The type of inclusion (Path, Query, Body, or Header) for the parameter.</param>
        public InAttribute(InclusionTypes InclusionType)
        {
            this.InclusionType = InclusionType;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="InAttribute"/> class with the specified inclusion type and custom name.
        /// </summary>
        /// <param name="InclusionType">The type of inclusion (Path, Query, Body, or Header) for the parameter.</param>
        /// <param name="name">The custom name to use for the parameter in the HTTP request/response instead of the C# parameter name.</param>
        protected InAttribute(InclusionTypes InclusionType, string name)
        {
            this.InclusionType = InclusionType;
            if(InclusionType != InclusionTypes.Header && InclusionType != InclusionTypes.Query)
                if(name != null)
                    throw new NotSupportedException("Custom names are not supported for path and body parameters.");

            Name = name;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="InAttribute"/> class with the default inclusion type of <see cref="InclusionTypes.Path"/>.
        /// </summary>
        public InAttribute()
        {
            InclusionType = InclusionTypes.Path;
        }

        /// <summary>
        /// Gets or sets the type of inclusion that determines where the parameter value should be placed in the HTTP request.
        /// </summary>
        /// <value>
        /// An <see cref="InclusionTypes"/> value specifying whether the parameter is in the path, query string, body, or headers.
        /// </value>
        public InclusionTypes InclusionType { get; set; }

        /// <summary>
        /// Gets or sets the custom name to use for the parameter in requests/responses.
        /// When set, this name will be used instead of the C# parameter name for query strings and headers.
        /// </summary>
        /// <value>
        /// A custom string name for the parameter, or <c>null</c> to use the C# parameter name.
        /// </value>
        /// <remarks>
        /// This property is typically overridden in derived classes to control visibility and behavior.
        /// For example, <see cref="InQueryAttribute"/> and <see cref="InHeaderAttribute"/> make this property visible
        /// and allow custom naming, while <see cref="InPathAttribute"/> and <see cref="InBodyAttribute"/> do not support it.
        /// </remarks>
        [EditorBrowsable(EditorBrowsableState.Never)] 
        public virtual string Name { get; set; }

        /// <summary>
        /// Add a description for the parameter to be used in generated documentation.
        /// </summary>
        public string Description { get; set; }
    }
}