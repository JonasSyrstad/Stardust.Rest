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

## HttpClient Management

### Default Provider (Backward Compatible)

By default, the framework uses a static dictionary to cache HttpClient instances per base URL. This prevents socket exhaustion but doesn't handle DNS changes.

### IHttpClientFactory (Recommended for Production)

For production workloads, use the IHttpClientFactory integration for proper connection lifetime management:

```csharp
// Program.cs or Startup.cs
services.AddStardustHttpClientFactory()
    .SetHandlerLifetime(TimeSpan.FromMinutes(15));  // DNS refresh interval

// Or with custom configuration
services.AddStardustHttpClientFactory(client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
    client.DefaultRequestHeaders.Add("User-Agent", "MyApp/1.0");
});
```

**Benefits of IHttpClientFactory:**
- ? DNS change handling through connection recycling
- ? Proper connection pooling with configurable lifetime
- ? Automatic disposal of expired connections
- ? Integration with Polly for resilience policies

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
ClientGlobalSettings.PooledConnectionLifetime = TimeSpan.FromMinutes(15);  // DNS refresh
ClientGlobalSettings.MaxConnectionsPerServer = 100;         // Connection limit

ProxyFactory.RunAuthProviderBeforeAppendingBody = true;     // Auth before body
ProxyFactory.EnableExpectContinue100ForPost = true;         // Expect: 100-continue
```

---

## ?? Security Considerations

The client library provides several built-in security features, but some security aspects require developer implementation.

### Built-in Security Features

| Feature | Status | Description |
|---------|--------|-------------|
| HTTP Header Injection Prevention | ? Built-in | Header names and values are automatically sanitized per RFC 7230 |
| HttpClient Connection Management | ? Built-in | IHttpClientFactory support for proper DNS handling |
| TLS/HTTPS | ? Uses System Default | Inherits .NET's default TLS configuration |

### Developer Responsibilities

#### ?? Authentication Handler Security

When implementing `IAuthenticationHandler`, follow these best practices:

```csharp
public class SecureBearerTokenHandler : IAuthenticationHandler
{
    private readonly ITokenProvider _tokenProvider;
    
    public SecureBearerTokenHandler(ITokenProvider tokenProvider)
    {
        _tokenProvider = tokenProvider ?? throw new ArgumentNullException(nameof(tokenProvider));
    }

    public async Task ApplyAsync(HttpRequestMessage req)
    {
        // ? Get token from secure provider (Azure Key Vault, DPAPI, etc.)
        var token = await _tokenProvider.GetTokenAsync();
        
        if (!string.IsNullOrEmpty(token))
        {
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }
    }

    // ...
}
```

**?? Security Checklist:**
- ? Never hardcode credentials in source code
- ? Never store tokens in plain text configuration
- ? Use secure token storage (Azure Key Vault, DPAPI, Credential Manager)
- ? Implement token refresh/rotation
- ? Handle 401 responses gracefully (re-authenticate and retry)

#### ?? Custom Header Handler Security

When implementing `IHeaderHandler`, ensure values are sanitized:

```csharp
public class SecureHeaderHandler : IHeaderHandler
{
    public void SetHeader(HttpRequestMessage req)
    {
        // ? Framework automatically sanitizes header values via SanitizeHttpHeaderValue()
        // ? Framework automatically validates header names via SanitizeHttpHeaderName()
        // ?? If bypassing framework methods, sanitize manually:
        
        var userInput = GetUserInput();
        // Remove CR/LF and other injection characters
        var sanitized = userInput?.Replace("\r", "").Replace("\n", "") ?? "";
        req.Headers.Add("X-Custom-Header", sanitized);
    }
}
```

#### ?? Response Deserialization Security

The framework uses Newtonsoft.Json for deserialization. For secure deserialization:

```csharp
// Configure secure JSON settings for a service type
var settings = new JsonSerializerSettings
{
    TypeNameHandling = TypeNameHandling.None,  // ?? Prevent type confusion attacks
    MaxDepth = 64                               // Prevent stack overflow
};

settings.AddClientSerializer<IMyService>();
```

**?? Security Checklist:**
- ? Set `TypeNameHandling = TypeNameHandling.None` (default)
- ? Validate expected response schemas
- ?? Be cautious with `IClientResponseInjector` implementations

#### ?? Connection Security

For production environments, use IHttpClientFactory with proper TLS configuration:

```csharp
services.AddStardustHttpClientFactory()
    .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
    {
        SslOptions = new SslClientAuthenticationOptions
        {
            EnabledSslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13,
            // Optional: Certificate pinning
            RemoteCertificateValidationCallback = ValidateCertificate
        },
        PooledConnectionLifetime = TimeSpan.FromMinutes(15),
        MaxConnectionsPerServer = 100
    });
```

### Security Testing Checklist

Before deploying to production:

- [ ] Authentication handler uses secure credential storage
- [ ] TLS 1.2+ enforced for all connections
- [ ] No sensitive data logged in error handlers
- [ ] Circuit breaker thresholds prevent DoS amplification
- [ ] Response size limits configured for large payload protection

---

## Target Framework

.NET Standard 2.0

## Dependencies

- Stardust.Interstellar.Rest.Annotations
- Microsoft.Extensions.DependencyInjection.Abstractions
- Microsoft.Extensions.Http (for IHttpClientFactory support)
- Newtonsoft.Json

## License

Apache-2.0

## Links

- [GitHub](https://github.com/JonasSyrstad/Stardust.Rest)
- [NuGet](https://www.nuget.org/packages/Stardust.Interstellar.Rest)
- [Security Assessment](docs/SECURITY_ASSESSMENT.md)
