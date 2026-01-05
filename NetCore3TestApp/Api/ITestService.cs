using System.Net;
using Stardust.Interstellar.Rest.Annotations;

namespace NetCore3TestApp.Api
{
    [Api("api/{id}")]
    public interface ITestClient
    {
        [Get("test")]
        [SuccessStatusCode(HttpStatusCode.Accepted)]
        Task<string> Test([InPath] string id, [InPath] string message);

        [Get("paramsTest")]
        Task<string> Params([InPath] string id, [InQuery] Dictionary<string, string> prefixedParameters);

        /// <summary>
        /// Client method demonstrating custom query parameter names.
        /// Will generate request: /api/{id}/search?q=text&page_size=10&sort_by=name
        /// </summary>
        [Get("search")]
        Task<string> Search(
            [InPath] string id,
            [InQuery("q")] string searchQuery,
            [InQuery("page_size")] int pageSize,
            [InQuery("sort_by")] string? sortBy);

        /// <summary>
        /// Client method demonstrating custom header names.
        /// Will send headers: X-Api-Key, X-Request-Id, Accept-Language
        /// </summary>
        [Get("header-test")]
        Task<string> HeaderTest(
            [InPath] string id,
            [InHeader("X-Api-Key")] string apiKey,
            [InHeader("X-Request-Id")] string? requestId,
            [InHeader("Accept-Language")] string? language);

        [Get("mixed")]
        Task<MvcMixedResult> MixedParams(
            [InPath] string id,
            [InQuery("filter_text")] string filterText,
            [InQuery("max_results")] int maxResults,
            [InHeader("Authorization")] string? authToken,
            [InHeader("X-Correlation-Id")] string? correlationId);
    }

    [Api("api/{id}")]
    public interface ITestService
    {
        [Get("test")]
        [SuccessStatusCode(HttpStatusCode.Accepted)]
        Task<string> Test([InPath] string id, [InPath] string message);

        [Get("test2")]
        Task<string> Test2([InPath] string id, [InPath] string message);

        [Get("test3")]
        [SuccessStatusCode(HttpStatusCode.Accepted)]
        string Test3([InPath] string id, [InPath] string message);

        [Get("test4")]
        string Test4([InPath] string id, [InPath] string message);

        [Post("test5")] 
        [SuccessStatusCode(HttpStatusCode.Accepted)]
        Task Test5([InPath] string id, [InQuery] string tt);

        [Get("paramsTest")]
        Task<string> Params([InPath] string id, [InQuery] Dictionary<string, string> prefixedParameters);

        /// <summary>
        /// Demonstrates custom query parameter names using InQuery with Name property.
        /// Request URL: /api/{id}/search?q=searchtext&page_size=10&sort_by=name&sort_order=asc
        /// </summary>
        [Get("search")]
        Task<MvcSearchResult> Search(
            [InPath] string id,
            [InQuery("q")] string searchQuery,
            [InQuery("page_size")] int pageSize,
            [InQuery("sort_by")] string? sortBy,
            [InQuery("sort_order")] string? sortOrder);

        /// <summary>
        /// Demonstrates custom header names using InHeader with Name property.
        /// Headers: X-Api-Key, X-Request-Id, X-Client-Version, Accept-Language
        /// </summary>
        [Get("header-test")]
        Task<MvcHeaderInfo> HeaderTest(
            [InPath] string id,
            [InHeader("X-Api-Key")] string apiKey,
            [InHeader("X-Request-Id")] string? requestId,
            [InHeader("X-Client-Version")] string? clientVersion,
            [InHeader("Accept-Language")] string? language);

        /// <summary>
        /// Demonstrates mixing custom query and header parameter names.
        /// Query: ?filter_text=text&max_results=100
        /// Headers: Authorization, X-Correlation-Id
        /// </summary>
        [Get("mixed")]
        Task<MvcMixedResult> MixedParams(
            [InPath] string id,
            [InQuery("filter_text")] string filterText,
            [InQuery("max_results")] int maxResults,
            [InHeader("Authorization")] string? authToken,
            [InHeader("X-Correlation-Id")] string? correlationId);
    }

    public class MvcSearchResult
    {
        public string Id { get; set; } = string.Empty;
        public string Query { get; set; } = string.Empty;
        public int PageSize { get; set; }
        public string? SortBy { get; set; }
        public string? SortOrder { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class MvcHeaderInfo
    {
        public string Id { get; set; } = string.Empty;
        public string ApiKey { get; set; } = string.Empty;
        public string? RequestId { get; set; }
        public string? ClientVersion { get; set; }
        public string? Language { get; set; }
        public DateTime ReceivedAt { get; set; }
    }

    public class MvcMixedResult
    {
        public string Id { get; set; } = string.Empty;
        public string FilterText { get; set; } = string.Empty;
        public int MaxResults { get; set; }
        public bool HasAuthToken { get; set; }
        public string? CorrelationId { get; set; }
    }

    public class TestService : ITestService
    {
        public Task<string> Test(string id, string message)
        {
            return Task.FromResult($"{DateTime.UtcNow}: '{message}' with id '{id}'");
        }

        public Task<string> Test2(string id, string message)
        {
            return Task.FromResult($"{DateTime.UtcNow}: {message}");
        }

        public string Test3(string id, string message)
        {
            return $"{DateTime.UtcNow}: {message}";
        }

        public string Test4(string id, string message)
        {
            return $"{DateTime.UtcNow}: {message}";
        }

        public Task Test5(string id, string tt) 
        {
            return Task.CompletedTask;
        }

        public Task<string> Params(string id, Dictionary<string, string> prefixedParameters)
        {
            return Task.FromResult(string.Join("&", prefixedParameters.Select(p => $"{p.Key}={p.Value}")) + " " + id);
        }

        public Task<MvcSearchResult> Search(string id, string searchQuery, int pageSize, string? sortBy, string? sortOrder)
        {
            return Task.FromResult(new MvcSearchResult
            {
                Id = id,
                Query = searchQuery,
                PageSize = pageSize,
                SortBy = sortBy,
                SortOrder = sortOrder,
                Message = $"Searching for '{searchQuery}' with page size {pageSize}, sorted by {sortBy ?? "default"} {sortOrder ?? "asc"}"
            });
        }

        public Task<MvcHeaderInfo> HeaderTest(string id, string apiKey, string? requestId, string? clientVersion, string? language)
        {
            return Task.FromResult(new MvcHeaderInfo
            {
                Id = id,
                ApiKey = apiKey,
                RequestId = requestId,
                ClientVersion = clientVersion,
                Language = language,
                ReceivedAt = DateTime.UtcNow
            });
        }

        public Task<MvcMixedResult> MixedParams(string id, string filterText, int maxResults, string? authToken, string? correlationId)
        {
            return Task.FromResult(new MvcMixedResult
            {
                Id = id,
                FilterText = filterText,
                MaxResults = maxResults,
                HasAuthToken = !string.IsNullOrEmpty(authToken),
                CorrelationId = correlationId
            });
        }
    }
}
