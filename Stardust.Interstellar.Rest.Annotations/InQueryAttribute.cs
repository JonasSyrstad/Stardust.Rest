using System;
using System.ComponentModel;

namespace Stardust.Interstellar.Rest.Annotations
{
    /// <summary>
    /// Marks a method parameter to be included in the HTTP query string.
    /// The parameter value will be serialized as a query string parameter in the URL.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Use <see cref="InQueryAttribute"/> when you want to pass data through the URL query string (e.g., ?name=value&amp;page=1).
    /// This is commonly used for filtering, pagination, search parameters, and optional data that doesn't belong in the URL path.
    /// </para>
    /// <para>
    /// <strong>When to use:</strong>
    /// </para>
    /// <list type="bullet">
    /// <item><description>Optional parameters that filter or modify the response</description></item>
    /// <item><description>Pagination parameters (page, pageSize, limit, offset)</description></item>
    /// <item><description>Search or filter criteria</description></item>
    /// <item><description>Sorting parameters</description></item>
    /// <item><description>Any data that should be visible in the URL and bookmarkable</description></item>
    /// </list>
    /// <para>
    /// <strong>Custom parameter names:</strong>
    /// You can specify a custom name for the query parameter using the constructor or the <see cref="Name"/> property.
    /// This is useful when your API follows naming conventions different from C# (e.g., snake_case or kebab-case).
    /// </para>
    /// <para>
    /// <strong>Complex types:</strong>
    /// Dictionary parameters are automatically flattened into multiple query string parameters.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Basic usage with C# parameter names
    /// [Api("api/products")]
    /// public interface IProductService
    /// {
    ///     [Get("search")]
    ///     Task&lt;List&lt;Product&gt;&gt; Search(
    ///         [InQuery] string name,
    ///         [InQuery] int pageSize,
    ///         [InQuery] int page);
    ///     // Generates: /api/products/search?name=laptop&amp;pageSize=10&amp;page=1
    /// }
    /// 
    /// // Using custom parameter names
    /// [Api("api/products")]
    /// public interface IProductService
    /// {
    ///     [Get("search")]
    ///     Task&lt;List&lt;Product&gt;&gt; Search(
    ///         [InQuery("q")] string searchQuery,
    ///         [InQuery("page_size")] int pageSize,
    ///         [InQuery("sort_by")] string sortBy);
    ///     // Generates: /api/products/search?q=laptop&amp;page_size=10&amp;sort_by=price
    /// }
    /// 
    /// // Using Dictionary for dynamic parameters
    /// [Api("api/data")]
    /// public interface IDataService
    /// {
    ///     [Get("filter")]
    ///     Task&lt;Data&gt; GetFiltered([InQuery] Dictionary&lt;string, string&gt; filters);
    ///     // Dictionary entries are flattened: ?color=red&amp;size=large&amp;brand=acme
    /// }
    /// </code>
    /// </example>
    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
    public class InQueryAttribute : InAttribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="InQueryAttribute"/> class.
        /// The query string parameter will use the C# parameter name.
        /// </summary>
        public InQueryAttribute() : base(InclutionTypes.Query)
        {
        }

        /// <summary>
        /// Marks a parameter to be included in the query string using a custom name or with a description.
        /// </summary>
        /// <param name="name">The custom name for the query parameter. Use null or empty to use the C# parameter name.</param>
        /// <param name="Description">Optional description for the parameter to be used in generated documentation.</param>
        /// <example>
        /// <code>
        /// // Parameter 'searchText' becomes 'q' in the query string with a description
        /// Task&lt;Results&gt; Search(
        ///     [InQuery("q", Description = "The search query text")] string searchText);
        /// // Generates: ?q=search+term
        /// 
        /// // Parameter uses C# name with only description
        /// Task&lt;Results&gt; Search(
        ///     [InQuery(Description = "The search query text")] string searchText);
        /// </code>
        /// </example>
        public InQueryAttribute(string name = null) : base(InclutionTypes.Query, name)
        {
        }

        /// <summary>
        /// Gets or sets the custom name to use for the query string parameter.
        /// When set, this name will be used instead of the C# parameter name.
        /// </summary>
        /// <value>
        /// The custom parameter name, or <c>null</c> to use the C# parameter name.
        /// </value>
        /// <remarks>
        /// This property allows you to align your API with specific naming conventions (e.g., snake_case, camelCase)
        /// without changing your C# code style.
        /// </remarks>
        [EditorBrowsable(EditorBrowsableState.Always)]
        public override string Name { get; set; }
    }
}