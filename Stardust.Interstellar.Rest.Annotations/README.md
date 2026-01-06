# Stardust.Interstellar.Rest.Annotations

> **Define once. Generate everywhere.**  
> Core attributes for building contract-first REST APIs in .NET.

[![NuGet](https://img.shields.io/nuget/v/Stardust.Interstellar.Rest.Annotations.svg)](https://www.nuget.org/packages/Stardust.Interstellar.Rest.Annotations)

---

## What is Stardust.Rest?

**Stardust.Rest** lets you define your REST API as a C# interface—then automatically generates both client proxies and server controllers. No more handwriting `HttpClient` boilerplate or scaffolding controllers.

```csharp
[Api("api/users")]
public interface IUserService
{
    [Get("{id}")]
    Task<User> GetAsync([InPath] string id);

    [Post]
    Task<User> CreateAsync([InBody] User user);
}
```

**That's it.** Share this interface between your client and server projects. The ecosystem handles the rest.

### Ecosystem Packages

| Package | What it does |
|---------|--------------|
| **[Annotations](https://www.nuget.org/packages/Stardust.Interstellar.Rest.Annotations)** | Define your API contract ? *you are here* |
| **[Client](https://www.nuget.org/packages/Stardust.Interstellar.Rest)** | Generate HTTP clients from interfaces |
| **[Server](https://www.nuget.org/packages/Stardust.Interstellar.Rest.Service.AspNetCore)** | Generate ASP.NET Core controllers from interfaces |

### Why use it?

- ?? **Contract-first** — One interface = perfect client/server alignment
- ? **Zero boilerplate** — No manual HTTP code or controller scaffolding
- ??? **Built-in resilience** — Circuit breakers, retries, error handling
- ?? **Extensible** — Custom auth, headers, serialization, error handlers

---

## Installation

```bash
dotnet add package Stardust.Interstellar.Rest.Annotations
```

---

## Quick Reference

### HTTP Verbs

```csharp
[Get("route")]      // GET request
[Post("route")]     // POST request
[Put("route")]      // PUT request
[Delete("route")]   // DELETE request
[Patch("route")]    // PATCH request
```

### Parameter Binding

```csharp
[Get("users/{id}")]
Task<User> GetUserAsync(
    [InPath] string id,                              // URL path segment
    [InQuery("page_size")] int pageSize,             // Query string (?page_size=10)
    [InHeader("X-Api-Key")] string apiKey,           // HTTP header
    [InBody] UpdateRequest request);                 // Request body
```

### Route Configuration

```csharp
[Api("api/v1/products")]                             // Base route prefix
[Api("api/users", "User Management API")]            // With description
```

---

## Attributes Reference

### Verb Attributes

| Attribute | HTTP Method |
|-----------|-------------|
| `[Get]` / `[Get("path")]` | GET |
| `[Post]` / `[Post("path")]` | POST |
| `[Put]` / `[Put("path")]` | PUT |
| `[Delete]` / `[Delete("path")]` | DELETE |
| `[Patch]` / `[Patch("path")]` | PATCH |
| `[Head]` / `[Head("path")]` | HEAD |
| `[Options]` / `[Options("path")]` | OPTIONS |

### Parameter Attributes

| Attribute | Location | Example |
|-----------|----------|---------|
| `[InPath]` | URL path | `/users/{id}` ? `[InPath] string id` |
| `[InQuery]` or `[InQuery("name")]` | Query string | `?filter=active` |
| `[InHeader]` or `[InHeader("X-Name")]` | HTTP header | Custom headers |
| `[InBody]` | Request body | JSON/XML payload |

All parameter attributes support `Description` for OpenAPI documentation:

```csharp
[InQuery("q", Description = "Search query text")] string searchQuery
```

### Resilience Attributes

```csharp
// Circuit breaker - stops calling failing services
[CircuitBreaker(threshold: 5, timeoutInMinutes: 1, resetTimeout: 2)]

// Retry with exponential backoff
[Retry(numberOfRetries: 3, retryInterval: 1000, incremetalWait: true)]

// Rate limiting
[Throttling(maxRequestsPerSecound: 10)]
```

### Circuit Breaker Scopes

```csharp
[CircuitBreaker(5, 1, 2)]                                    // Shared (default)
[CircuitBreaker(5, 1, 2, CircuitBreakerScope.User)]          // Per user
[CircuitBreaker(5, 1, 2, CircuitBreakerScope.Client)]        // Per client
[CircuitBreaker(5, 1, 2, CircuitBreakerScope.UserAndClient)] // Multi-tenant
```

### Authorization

```csharp
[AuthorizeWrapper]                        // Require authentication
[AuthorizeWrapper(Roles = "Admin")]       // Require role
[AuthorizeWrapper(Policy = "CanDelete")]  // Require policy
```

### Response Handling

```csharp
[SuccessStatusCode(HttpStatusCode.Created)]    // Return 201 on success
[SuccessStatusCode(HttpStatusCode.NoContent)]  // Return 204 on success
```

### API Versioning

```csharp
[Api("api/users")]
[Version("1.0")]
public interface IUserServiceV1 { }

[Api("api/users")]
[Version("2.0", Deprecated = true)]
public interface IUserServiceV2 { }
```

### OpenAPI Documentation

```csharp
[Api("api/users")]
[ServiceDescription("User management", Tags = "Users", Summary = "CRUD operations")]
public interface IUserService
{
    [Get("{id}", "Get user by ID")]
    Task<User> GetAsync([InPath(Description = "Unique user ID")] string id);
}
```

### Endpoint Routing Policies (.NET 6+)

```csharp
[Api("api/items")]
[EndpointRoutingPolicy("SecurityHeaders")]  // Apply to all endpoints
public interface IItemService
{
    [Get]
    [EndpointRoutingPolicy("Caching")]      // Endpoint-specific
    Task<List<Item>> GetItemsAsync();
}
```

---

## Extensibility Interfaces

### IAuthenticationHandler

Custom authentication logic:

```csharp
public class BearerTokenHandler : IAuthenticationHandler
{
    public void Apply(HttpRequestMessage req)
    {
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _token);
    }
    // ... other members
}
```

### IHeaderHandler

Custom header processing:

```csharp
public class CorrelationIdHandler : IHeaderHandler
{
    public void SetHeader(HttpRequestMessage req)
    {
        req.Headers.Add("X-Correlation-Id", Guid.NewGuid().ToString());
    }
    // ... other members
}
```

### IErrorHandler

Custom error handling:

```csharp
public class CustomErrorHandler : IErrorHandler
{
    public Exception ProduceClientException(
        string message, HttpStatusCode statusCode, 
        Exception innerException, string body)
    {
        return new CustomApiException(message, statusCode, body);
    }
}

[ErrorHandler(typeof(CustomErrorHandler))]
public interface IApiService { }
```

### IErrorCategorizer

Control retry behavior:

```csharp
public class CustomErrorCategorizer : IErrorCategorizer
{
    public bool IsTransientError(Exception ex) => ex is HttpRequestException { StatusCode: HttpStatusCode.ServiceUnavailable };
}

[Retry(3, 1000, true, ErrorCategorizer = typeof(CustomErrorCategorizer))]
public interface IService { }
```

---

## Target Frameworks

.NET Standard 2.0 | .NET 6.0 | .NET 7.0 | .NET 8.0

## License

Apache-2.0

## Links

- [GitHub](https://github.com/JonasSyrstad/Stardust.Rest)
- [NuGet](https://www.nuget.org/packages/Stardust.Interstellar.Rest.Annotations)
