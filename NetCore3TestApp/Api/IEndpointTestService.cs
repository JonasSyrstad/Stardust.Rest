using System.Net;
using Stardust.Interstellar.Rest.Annotations;
using Stardust.Interstellar.Rest.Annotations.EndpointRouting;

namespace NetCore3TestApp.Api
{
    /// <summary>
    /// Test service interface for endpoint routing tests.
    /// </summary>
    [Api("api/endpoints", "Endpoint Test API", Tags = "Endpoints", Summary = "API for testing endpoint routing functionality")]
    [EndpointRoutingPolicy("SecurityHeaders")] // Apply security headers to all endpoints
    public interface IEndpointTestService
    {
        [Get("hello", "Get Hello", Summary = "Returns a simple hello message")]
        [EndpointRoutingPolicy("Caching")] // This endpoint supports caching
        Task<string> GetHelloAsync();

        [Get("hello/{name}", "Get Hello with Name", Summary = "Returns a personalized hello message")]
        Task<string> GetHelloWithNameAsync([InPath(Description = "The name to include in the greeting")] string name);

        [Get("items", "Get All Items", Summary = "Returns all items in the collection")]
        [EndpointRoutingPolicy("Caching")] // Cache the items list
        [EndpointRoutingPolicy("RateLimiting")] // Rate limit this endpoint
        Task<List<TestItem>> GetItemsAsync();

        [Get("items/{id}", "Get Item by ID", Summary = "Returns a specific item by its ID")]
        [EndpointRoutingPolicy("Caching")]
        Task<TestItem> GetItemByIdAsync([InPath(Description = "The unique identifier of the item to retrieve")] int id);

        [Post("items", "Create Item", Summary = "Creates a new item")]
        [SuccessStatusCode(HttpStatusCode.Created)]
        [EndpointRoutingPolicy("RateLimiting")] // Rate limit write operations
        Task<TestItem> CreateItemAsync([In(InclusionTypes.Body, Description = "The item to create with name and description")] TestItem item);

        [Put("items/{id}", "Update Item", Summary = "Updates an existing item")]
        [EndpointRoutingPolicy("RateLimiting")]
        Task<TestItem> UpdateItemAsync(
            [InPath(Description = "The unique identifier of the item to update")] int id, 
            [In(InclusionTypes.Body, Description = "The updated item data")] TestItem item);

        [Delete("items/{id}", "Delete Item", Summary = "Deletes an item by its ID")]
        [SuccessStatusCode(HttpStatusCode.NoContent)]
        [EndpointRoutingPolicy("RateLimiting")]
        Task DeleteItemAsync([InPath(Description = "The unique identifier of the item to delete")] int id);

        [Get("query", "Query with Parameters", Summary = "Demonstrates query string parameter binding")]
        Task<string> GetWithQueryParamsAsync(
            [InQuery(Description = "Optional filter text to apply to results")] string? filter, 
            [InQuery(Description = "Optional page number for pagination (1-based)")] int? page);

        [Get("headers", "Header Parameter Test", Summary = "Demonstrates header parameter binding")]
        Task<string> GetWithHeaderParamAsync(
            [InHeader(Description = "Optional custom header value to echo back")] string? customHeader);

        [Get("sync", "Synchronous Method", Summary = "Demonstrates a synchronous method")]
        string GetSyncMethod();

        [Post("void", "Void Return", Summary = "Demonstrates a void-returning async method")]
        [SuccessStatusCode(HttpStatusCode.NoContent)]
        Task PostVoidAsync([In(InclusionTypes.Body, Description = "The item to process (will not be persisted)")] TestItem item);

        [Get("versioned", "Versioned Endpoint", Summary = "Demonstrates API versioning policy")]
        [EndpointRoutingPolicy("ApiVersion")]
        Task<string> GetVersionedEndpointAsync();

        [Get("cors-enabled", "CORS Enabled Endpoint", Summary = "Demonstrates CORS policy headers")]
        [EndpointRoutingPolicy("ApiCors")]
        Task<string> GetCorsEnabledEndpointAsync();

        /// <summary>
        /// Demonstrates custom query parameter names using InQuery with Name property.
        /// The C# parameters use camelCase but the API uses snake_case query parameters.
        /// Example: /api/endpoints/search?q=test&page_size=10&sort_by=name&sort_order=asc
        /// </summary>
        [Get("search", "Search with Custom Query Names", Summary = "Demonstrates InQuery with custom parameter names")]
        Task<SearchResult> SearchAsync(
            [InQuery("q", Description = "Search query text to filter items by name or description")] string searchQuery,
            [InQuery("page_size", Description = "Number of results to return per page (default: 10)")] int pageSize,
            [InQuery("sort_by", Description = "Field name to sort results by (options: 'name', 'id')")] string? sortBy,
            [InQuery("sort_order", Description = "Sort direction: 'asc' for ascending or 'desc' for descending (default: 'asc')")] string? sortOrder);

        /// <summary>
        /// Demonstrates custom header names using InHeader with Name property.
        /// Maps C# parameter names to standard HTTP header names.
        /// </summary>
        [Get("custom-headers", "Custom Header Names", Summary = "Demonstrates InHeader with custom header names")]
        Task<HeaderInfo> GetWithCustomHeadersAsync(
            [InHeader("X-Api-Key", Description = "API key for authentication (required for production use)")] string apiKey,
            [InHeader("X-Request-Id", Description = "Unique request identifier for tracking and correlation across services")] string? requestId,
            [InHeader("X-Client-Version", Description = "Client application version for compatibility checking (format: 'major.minor.patch')")] string? clientVersion,
            [InHeader("Accept-Language", Description = "Preferred language for localized responses (RFC 5646 language tag, e.g., 'en-US', 'fr-FR')")] string? language);

        /// <summary>
        /// Demonstrates mixing custom-named query and header parameters.
        /// </summary>
        [Get("mixed-params", "Mixed Custom Parameters", Summary = "Demonstrates mixing custom query and header names")]
        Task<MixedParamsResult> GetWithMixedParamsAsync(
            [InQuery("$filter", Description = "Text filter to apply to results (case-insensitive substring match)")] string filterText,
            [InQuery("max_results", Description = "Maximum number of results to return (range: 1-1000, default: 100)")] int maxResults,
            [InHeader("Authorization", Description = "Bearer token for API authentication (format: 'Bearer {token}')")] string? authToken,
            [InHeader("X-Correlation-Id", Description = "Correlation identifier for distributed tracing and log aggregation")] string? correlationId);
    }

    public class TestItem
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }

    public class SearchResult
    {
        public string Query { get; set; } = string.Empty;
        public int PageSize { get; set; }
        public string? SortBy { get; set; }
        public string? SortOrder { get; set; }
        public List<TestItem> Items { get; set; } = [];
        public int TotalCount { get; set; }
    }

    public class HeaderInfo
    {
        public string ApiKey { get; set; } = string.Empty;
        public string? RequestId { get; set; }
        public string? ClientVersion { get; set; }
        public string? Language { get; set; }
        public DateTime ReceivedAt { get; set; }
    }

    public class MixedParamsResult
    {
        public string FilterText { get; set; } = string.Empty;
        public int MaxResults { get; set; }
        public bool IsAuthenticated { get; set; }
        public string? CorrelationId { get; set; }
    }

    public class EndpointTestService : IEndpointTestService
    {
        private static readonly List<TestItem> _items =
        [
            new TestItem { Id = 1, Name = "Item 1", Description = "First item" },
            new TestItem { Id = 2, Name = "Item 2", Description = "Second item" },
            new TestItem { Id = 3, Name = "Item 3", Description = "Third item" }
        ];

        public Task<string> GetHelloAsync()
        {
            return Task.FromResult("Hello from Endpoint Routing!");
        }

        public Task<string> GetHelloWithNameAsync(string name)
        {
            return Task.FromResult($"Hello, {name}!");
        }

        public Task<List<TestItem>> GetItemsAsync()
        {
            return Task.FromResult(_items);
        }

        public Task<TestItem> GetItemByIdAsync(int id)
        {
            var item = _items.Find(i => i.Id == id) 
                ?? throw new KeyNotFoundException($"Item with id {id} not found");
            return Task.FromResult(item);
        }

        public Task<TestItem> CreateItemAsync(TestItem item)
        {
            item.Id = _items.Count + 1;
            _items.Add(item);
            return Task.FromResult(item);
        }

        public Task<TestItem> UpdateItemAsync(int id, TestItem item)
        {
            var existingIndex = _items.FindIndex(i => i.Id == id);
            if (existingIndex < 0)
            {
                throw new KeyNotFoundException($"Item with id {id} not found");
            }
            item.Id = id;
            _items[existingIndex] = item;
            return Task.FromResult(item);
        }

        public Task DeleteItemAsync(int id)
        {
            var item = _items.Find(i => i.Id == id) 
                ?? throw new KeyNotFoundException($"Item with id {id} not found");
            _items.Remove(item);
            return Task.CompletedTask;
        }

        public Task<string> GetWithQueryParamsAsync(string? filter, int? page)
        {
            return Task.FromResult($"Filter: {filter ?? "none"}, Page: {page?.ToString() ?? "none"}");
        }

        public Task<string> GetWithHeaderParamAsync(string? customHeader)
        {
            return Task.FromResult($"Header value: {customHeader ?? "none"}");
        }

        public string GetSyncMethod()
        {
            return "Synchronous response";
        }

        public Task PostVoidAsync(TestItem item)
        {
            return Task.CompletedTask;
        }

        public Task<string> GetVersionedEndpointAsync()
        {
            return Task.FromResult("This is a versioned endpoint (v1.0)");
        }

        public Task<string> GetCorsEnabledEndpointAsync()
        {
            return Task.FromResult("This endpoint has CORS headers enabled");
        }

        public Task<SearchResult> SearchAsync(string searchQuery, int pageSize, string? sortBy, string? sortOrder)
        {
            // Filter items based on search query
            var filtered = string.IsNullOrEmpty(searchQuery) 
                ? _items 
                : _items.Where(i => i.Name.Contains(searchQuery, StringComparison.OrdinalIgnoreCase) 
                    || i.Description.Contains(searchQuery, StringComparison.OrdinalIgnoreCase)).ToList();

            // Apply sorting
            if (!string.IsNullOrEmpty(sortBy))
            {
                filtered = sortBy.ToLowerInvariant() switch
                {
                    "name" => sortOrder?.ToLowerInvariant() == "desc" 
                        ? filtered.OrderByDescending(i => i.Name).ToList() 
                        : filtered.OrderBy(i => i.Name).ToList(),
                    "id" => sortOrder?.ToLowerInvariant() == "desc" 
                        ? filtered.OrderByDescending(i => i.Id).ToList() 
                        : filtered.OrderBy(i => i.Id).ToList(),
                    _ => filtered
                };
            }

            return Task.FromResult(new SearchResult
            {
                Query = searchQuery,
                PageSize = pageSize,
                SortBy = sortBy,
                SortOrder = sortOrder,
                Items = filtered.Take(pageSize).ToList(),
                TotalCount = filtered.Count
            });
        }

        public Task<HeaderInfo> GetWithCustomHeadersAsync(string apiKey, string? requestId, string? clientVersion, string? language)
        {
            return Task.FromResult(new HeaderInfo
            {
                ApiKey = apiKey,
                RequestId = requestId,
                ClientVersion = clientVersion,
                Language = language,
                ReceivedAt = DateTime.UtcNow
            });
        }

        public Task<MixedParamsResult> GetWithMixedParamsAsync(string filterText, int maxResults, string? authToken, string? correlationId)
        {
            return Task.FromResult(new MixedParamsResult
            {
                FilterText = filterText,
                MaxResults = maxResults,
                IsAuthenticated = !string.IsNullOrEmpty(authToken),
                CorrelationId = correlationId
            });
        }
    }
}
