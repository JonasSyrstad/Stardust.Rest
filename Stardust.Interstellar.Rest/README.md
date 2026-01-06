# Stardust.Interstellar.Rest

> **Define once. Generate everywhere.**  
> Generate strongly-typed HTTP clients from decorated interfaces.

[![NuGet](https://img.shields.io/nuget/v/Stardust.Interstellar.Rest.svg)](https://www.nuget.org/packages/Stardust.Interstellar.Rest)

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
| **[Annotations](https://www.nuget.org/packages/Stardust.Interstellar.Rest.Annotations)** | Define your API contract |
| **[Client](https://www.nuget.org/packages/Stardust.Interstellar.Rest)** | Generate HTTP clients from interfaces ? *you are here* |
| **[Server](https://www.nuget.org/packages/Stardust.Interstellar.Rest.Service.AspNetCore)** | Generate ASP.NET Core controllers from interfaces |

### Why use it?

- ?? **Contract-first** — One interface = perfect client/server alignment
- ? **Zero boilerplate** — No manual HTTP code or controller scaffolding
- ??? **Built-in resilience** — Circuit breakers, retries, error handling
- ?? **Extensible** — Custom auth, headers, serialization, error handlers

---

## Installation

```bash
dotnet add package Stardust.Interstellar.Rest
```

---

## Quick Start

### 1. Define your interface

```csharp
using Stardust.Interstellar.Rest.Annotations;

[Api("api/users")]
public interface IUserService
{
    [Get("{id}")]
    Task<User> GetUserAsync([InPath] string id);

    [Get]
    Task<IEnumerable<User>> GetAllAsync();

    [Post]
    Task<User> CreateAsync([InBody] User user);

    [Put("{id}")]
    Task<User> UpdateAsync([InPath] string id, [InBody] User user);

    [Delete("{id}")]
    Task DeleteAsync([InPath] string id);
}
```

### 2. Register the client

```csharp
services.AddInterstellar();
services.AddTransient(sp => sp.CreateRestClient<IUserService>("https://api.example.com"));
```

### 3. Use it

```csharp
public class MyController
{
    private readonly IUserService _users;
    public MyController(IUserService users) => _users = users;

    public async Task<User> GetUser(string id) => await _users.GetUserAsync(id);
}
```

---

## Creating Clients

### Basic registration

```csharp
services.AddTransient(sp => sp.CreateRestClient<IUserService>("https://api.example.com"));
```

### With extras collector

Capture request metadata:

```csharp
services.AddTransient(sp => sp.CreateRestClient<IUserService>(
    "https://api.example.com",
    extras => {
        var retryCount = extras.ContainsKey("retryCount") ? extras["retryCount"] : 0;
    }));
```

### Using IProxyFactory

```csharp
public class MyService
{
    private readonly IProxyFactory _factory;
    public MyService(IProxyFactory factory) => _factory = factory;

    public async Task DoWork()
    {
        var client = _factory.CreateInstance<IExternalApi>("https://api.external.com");
        var result = await client.GetDataAsync();
    }
}
```

---

## Parameter Binding

```csharp
[Get("search")]
Task<SearchResult> SearchAsync(
    [InQuery("q")] string searchQuery,           // ?q=laptop
    [InQuery("page_size")] int pageSize,         // ?page_size=10
    [InHeader("X-Api-Key")] string apiKey);      // X-Api-Key header

[Get("filter")]
Task<Data> GetFiltered([InQuery] Dictionary<string, string> filters);
// Expands to: ?key1=value1&key2=value2
```

---

## Resilience

### Circuit Breaker

Prevents cascading failures by stopping calls to failing services:

```csharp
[CircuitBreaker(
    threshold: 5,           // Failures before opening
    timeoutInMinutes: 1,    // Time circuit stays open
    resetTimeout: 2)]       // Time before half-open
public interface IExternalService { }
```

**Scoped circuit breakers** isolate failures by user, client, or both:

```csharp
[CircuitBreaker(5, 1, 2, CircuitBreakerScope.User)]          // Per user
[CircuitBreaker(5, 1, 2, CircuitBreakerScope.Client)]        // Per client
[CircuitBreaker(5, 1, 2, CircuitBreakerScope.UserAndClient)] // Multi-tenant
```

### Retry Policies

Automatic retry with exponential backoff:

```csharp
[Retry(numberOfRetries: 3, retryInterval: 1000, incremetalWait: true)]
public interface IResilientService { }
```

Custom error categorization:

```csharp
public class TransientErrorCategorizer : IErrorCategorizer
{
    public bool IsTransientError(Exception ex) => ex is HttpRequestException 
        { StatusCode: HttpStatusCode.ServiceUnavailable or HttpStatusCode.TooManyRequests };
}

[Retry(3, 1000, true, ErrorCategorizer = typeof(TransientErrorCategorizer))]
public interface IService { }
```

---

## Authentication

Implement `IAuthenticationHandler`:

```csharp
public class BearerTokenHandler : IAuthenticationHandler
{
    private readonly string _token;
    public BearerTokenHandler(string token) => _token = token;

    public void Apply(HttpRequestMessage req)
    {
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _token);
    }

    public Task ApplyAsync(HttpRequestMessage req) { Apply(req); return Task.CompletedTask; }
    public void BodyData(byte[] body) { } // For signature-based auth (HMAC)
}

// Register
services.AddScoped<IAuthenticationHandler, BearerTokenHandler>();
```

---

## Header Handling

Custom headers via `IHeaderHandler`:

```csharp
public class CorrelationIdHandler : IHeaderHandler
{
    public int ProcessingOrder => 0;
    public void SetHeader(HttpRequestMessage req)
    {
        req.Headers.Add("X-Correlation-Id", Activity.Current?.Id ?? Guid.NewGuid().ToString());
    }
    // ... other interface members
}

services.AddScoped<IHeaderHandler, CorrelationIdHandler>();
```

---

## Runtime Configuration

Fluent configuration on client instances:

```csharp
var client = sp.CreateRestClient<IUserService>("https://api.example.com");

client
    .AddHeaderValue("X-Custom-Header", "value")
    .SetVersion("v2")                              // Path: /v2/api/users
    .SetQueryStringVersion("api-version", "2.0")   // Query: ?api-version=2.0
    .SetHeaderVersion("X-API-Version", "2.0")      // Header version
    .SetProxy(new WebProxy("http://proxy:8080"));
```

---

## Error Handling

All HTTP errors are wrapped in `RestWrapperException`:

```csharp
try
{
    var user = await userService.GetUserAsync("123");
}
catch (RestWrapperException ex)
{
    Console.WriteLine($"Status: {ex.StatusCode}");
    Console.WriteLine($"Body: {ex.Value}");
}
```

Custom error handler:

```csharp
public class ApiErrorHandler : IErrorHandler
{
    public Exception ProduceClientException(
        string message, HttpStatusCode statusCode, 
        Exception innerException, string responseBody)
    {
        var error = JsonSerializer.Deserialize<ApiError>(responseBody);
        return new ApiException(error.Code, error.Message, statusCode);
    }
}

[ErrorHandler(typeof(ApiErrorHandler))]
public interface IApiService { }
```

---

## Serialization

**JSON** (default) uses Newtonsoft.Json. **XML** is also supported:

```csharp
[UseXml]
public interface IXmlService { }
```

---

## Global Settings

```csharp
ClientGlobalSettings.Timeout = 30;                          // HTTP timeout (seconds)
ProxyFactory.RunAuthProviderBeforeAppendingBody = true;     // Auth before body
ProxyFactory.EnableExpectContinue100ForPost = true;         // Expect: 100-continue
```

---

## Target Framework

.NET Standard 2.0

## Dependencies

- Stardust.Interstellar.Rest.Annotations
- Microsoft.Extensions.DependencyInjection.Abstractions
- Newtonsoft.Json

## License

Apache-2.0

## Links

- [GitHub](https://github.com/JonasSyrstad/Stardust.Rest)
- [NuGet](https://www.nuget.org/packages/Stardust.Interstellar.Rest)
