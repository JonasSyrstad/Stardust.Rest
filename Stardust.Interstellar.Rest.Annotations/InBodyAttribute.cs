using System;
using System.ComponentModel;

namespace Stardust.Interstellar.Rest.Annotations
{
    /// <summary>
    /// Marks a method parameter to be included in the request body.
    /// The parameter value will be serialized and sent in the HTTP request body, typically as JSON.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Use <see cref="InBodyAttribute"/> for complex objects or data that should be sent in the request body.
    /// This is the standard way to send data for POST, PUT, and PATCH operations.
    /// </para>
    /// <para>
    /// <strong>When to use:</strong>
    /// </para>
    /// <list type="bullet">
    /// <item><description>Creating new resources (POST requests with entity data)</description></item>
    /// <item><description>Updating existing resources (PUT/PATCH requests with entity data)</description></item>
    /// <item><description>Sending complex objects with multiple properties</description></item>
    /// <item><description>Sending large amounts of data that exceed URL length limits</description></item>
    /// <item><description>Sending data that shouldn't be visible in URLs or logs</description></item>
    /// </list>
    /// <para>
    /// <strong>Limitations:</strong>
    /// </para>
    /// <list type="bullet">
    /// <item><description>Custom parameter names are not supported for body parameters (the <see cref="Name"/> property will throw <see cref="NotSupportedException"/>)</description></item>
    /// <item><description>Typically only one parameter per method should use <see cref="InBodyAttribute"/></description></item>
    /// <item><description>Not typically used with GET or DELETE requests</description></item>
    /// </list>
    /// <para>
    /// <strong>Serialization:</strong>
    /// The parameter object is automatically serialized to JSON (or other configured format) before being sent.
    /// All public properties of the object will be included in the serialization.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Basic usage - creating a new user
    /// [Api("api/users")]
    /// public interface IUserService
    /// {
    ///     [Post("")]
    ///     Task&lt;User&gt; CreateUser([InBody] User newUser);
    ///     // Request body: { "name": "John", "email": "john@example.com" }
    /// }
    /// 
    /// // Updating an existing resource
    /// [Api("api/products")]
    /// public interface IProductService
    /// {
    ///     [Put("{id}")]
    ///     Task&lt;Product&gt; UpdateProduct(
    ///         [InPath] string id,
    ///         [InBody] Product updatedProduct);
    ///     // URL: /api/products/123
    ///     // Body: { "name": "New Name", "price": 99.99 }
    /// }
    /// 
    /// // Mixing body with other parameter types
    /// [Api("api/orders")]
    /// public interface IOrderService
    /// {
    ///     [Post("")]
    ///     Task&lt;Order&gt; CreateOrder(
    ///         [InBody] Order order,
    ///         [InHeader("X-Api-Key")] string apiKey,
    ///         [InQuery] bool sendNotification);
    ///     // Body contains the order object
    ///     // Header: X-Api-Key: abc123
    ///     // Query: ?sendNotification=true
    /// }
    /// 
    /// // Complex nested objects
    /// [Api("api/checkout")]
    /// public interface ICheckoutService
    /// {
    ///     [Post("process")]
    ///     Task&lt;CheckoutResult&gt; ProcessCheckout([InBody] CheckoutRequest request);
    ///     // Request body can contain nested objects:
    ///     // {
    ///     //   "cart": { "items": [...], "total": 150.00 },
    ///     //   "payment": { "method": "card", "token": "..." },
    ///     //   "shipping": { "address": {...}, "method": "express" }
    ///     // }
    /// }
    /// </code>
    /// </example>
	[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
	public class InBodyAttribute : InAttribute
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="InBodyAttribute"/> class.
		/// </summary>
		public InBodyAttribute() : base(InclutionTypes.Body)
		{

		}

		/// <summary>
		/// Initializes a new instance of the <see cref="InBodyAttribute"/> class with a description.
		/// </summary>
		/// <param name="Description">The description for the parameter to be used in generated documentation.</param>
		public InBodyAttribute(string Description) : base(InclutionTypes.Body)
		{
			this.Description = Description;
		}

        /// <summary>
        /// Gets or sets the custom name for the body parameter.
        /// This property is not supported for body parameters and will throw <see cref="NotSupportedException"/> when accessed.
        /// </summary>
        /// <value>Always returns <c>null</c>.</value>
        /// <exception cref="NotSupportedException">Always thrown when attempting to set this property.</exception>
        /// <remarks>
        /// Body parameters do not support custom naming because the entire parameter object is serialized as the request body.
        /// The object's property names are preserved during serialization and determine the JSON structure.
        /// </remarks>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public override string Name
        {
            get => null;
            set => throw new NotSupportedException("Not supported for body parameter names");
        }
    }
}