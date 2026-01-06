using System;
using System.ComponentModel;

namespace Stardust.Interstellar.Rest.Annotations
{
    /// <summary>
    /// Marks a method parameter to be included in the URL path.
    /// The parameter value will be substituted into a placeholder in the route template.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Use <see cref="InPathAttribute"/> when you need to identify a specific resource in the URL path.
    /// Path parameters are part of the URL structure and are typically used for resource identifiers.
    /// </para>
    /// <para>
    /// <strong>When to use:</strong>
    /// </para>
    /// <list type="bullet">
    /// <item><description>Resource identifiers (e.g., user ID, product ID, order number)</description></item>
    /// <item><description>Hierarchical resource paths (e.g., /api/users/{userId}/orders/{orderId})</description></item>
    /// <item><description>Required parameters that define the resource being accessed</description></item>
    /// <item><description>Values that should be part of the URL structure and RESTful design</description></item>
    /// </list>
    /// <para>
    /// <strong>Route template matching:</strong>
    /// The parameter name must match a placeholder in the route template defined by verb attributes (e.g., <c>[Get("{id}")]</c>).
    /// The framework will substitute the parameter value into the URL path at runtime.
    /// </para>
    /// <para>
    /// <strong>Limitations:</strong>
    /// </para>
    /// <list type="bullet">
    /// <item><description>Custom parameter names are not supported (the <see cref="Name"/> property will throw <see cref="NotSupportedException"/>)</description></item>
    /// <item><description>The parameter name must exactly match the placeholder name in the route template</description></item>
    /// <item><description>Path parameters cannot be optional - they must have a value</description></item>
    /// </list>
    /// <para>
    /// <strong>Default behavior:</strong>
    /// If no <c>InAttribute</c> is specified on a parameter, it defaults to <see cref="InclutionTypes.Path"/>.
    /// However, it's recommended to be explicit by using <see cref="InPathAttribute"/> for clarity.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Basic usage - single path parameter
    /// [Api("api/users")]
    /// public interface IUserService
    /// {
    ///     [Get("{id}")]
    ///     Task&lt;User&gt; GetUser([InPath] string id);
    ///     // Generates: /api/users/123
    ///     
    ///     [Delete("{id}")]
    ///     Task DeleteUser([InPath] string id);
    ///     // Generates: /api/users/123
    /// }
    /// 
    /// // Multiple path parameters - hierarchical resources
    /// [Api("api/organizations")]
    /// public interface IOrganizationService
    /// {
    ///     [Get("{orgId}/departments/{deptId}")]
    ///     Task&lt;Department&gt; GetDepartment(
    ///         [InPath] string orgId,
    ///         [InPath] string deptId);
    ///     // Generates: /api/organizations/org123/departments/dept456
    /// }
    /// 
    /// // Mixing path parameters with other types
    /// [Api("api/products")]
    /// public interface IProductService
    /// {
    ///     [Get("{categoryId}/items")]
    ///     Task&lt;List&lt;Product&gt;&gt; GetProducts(
    ///         [InPath] string categoryId,
    ///         [InQuery] int page,
    ///         [InQuery] int pageSize);
    ///     // Generates: /api/products/electronics/items?page=1&amp;pageSize=20
    ///     
    ///     [Put("{id}")]
    ///     Task&lt;Product&gt; UpdateProduct(
    ///         [InPath] string id,
    ///         [InBody] Product product);
    ///     // URL: /api/products/123
    ///     // Body: { "name": "Updated Product", "price": 99.99 }
    /// }
    /// 
    /// // Complex path structures
    /// [Api("api/{tenantId}/users")]
    /// public interface ITenantUserService
    /// {
    ///     [Get("{userId}/profile")]
    ///     Task&lt;UserProfile&gt; GetProfile(
    ///         [InPath] string tenantId,
    ///         [InPath] string userId);
    ///     // Generates: /api/tenant123/users/user456/profile
    /// }
    /// </code>
    /// </example>
	[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
	public class InPathAttribute : InAttribute
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="InPathAttribute"/> class.
		/// </summary>
		public InPathAttribute():base(InclutionTypes.Path)
		{
            
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="InPathAttribute"/> class with a description.
		/// </summary>
		/// <param name="Description">The description for the parameter to be used in generated documentation.</param>
		public InPathAttribute(string Description):base(InclutionTypes.Path)
		{
            this.Description = Description;
		}

        /// <summary>
        /// Gets or sets the custom name for the path parameter.
        /// This property is not supported for path parameters and will throw <see cref="NotSupportedException"/> when accessed.
        /// </summary>
        /// <value>Always returns <c>null</c>.</value>
        /// <exception cref="NotSupportedException">Always thrown when attempting to set this property.</exception>
        /// <remarks>
        /// Path parameters do not support custom naming because the parameter name must exactly match 
        /// the placeholder name in the route template (e.g., <c>{id}</c> in <c>[Get("{id}")]</c>).
        /// Renaming is not supported to maintain consistency between the C# parameter name and the URL structure.
        /// </remarks>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public virtual string Name
        {
            get => null;
            set => throw new NotSupportedException("Not supported for path parameter names");
        }
    }
}