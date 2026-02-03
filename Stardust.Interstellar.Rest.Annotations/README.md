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

## ?? Security Considerations for Extensibility Interfaces

When implementing the extensibility interfaces, you take responsibility for certain security aspects. Follow these guidelines to ensure your implementations are secure.

### ?? IAuthenticationHandler Security

**Your Responsibilities:**
- Secure credential storage and retrieval
- Token lifecycle management (refresh, rotation, expiration)
- Protection against credential leakage

```csharp
/// <summary>
/// SECURITY: Implementations must:
/// - Never hardcode credentials
/// - Use secure storage (Azure Key Vault, DPAPI, etc.)
/// - Implement proper token refresh logic
/// - Clear sensitive data from memory when possible
/// </summary>
public class SecureAuthHandler : IAuthenticationHandler
{
    private readonly ISecureTokenProvider _tokenProvider;
    
    public SecureAuthHandler(ISecureTokenProvider tokenProvider)
    {
        _tokenProvider = tokenProvider ?? throw new ArgumentNullException(nameof(tokenProvider));
    }

    public async Task ApplyAsync(HttpRequestMessage req)
    {
        // ? Get token from secure provider
        var token = await _tokenProvider.GetTokenAsync();
        
        if (!string.IsNullOrEmpty(token))
        {
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }
    }

    public void Apply(HttpRequestMessage req)
    {
        ApplyAsync(req).GetAwaiter().GetResult();
    }

    public void BodyData(byte[] body)
    {
        // For HMAC-based auth: compute signature over body
        // ?? Ensure signing key is securely stored
    }
}
```

**?? Anti-patterns to avoid:**
```csharp
// ? DON'T: Hardcoded credentials
public class InsecureHandler : IAuthenticationHandler
{
    private const string ApiKey = "sk-12345...";  // ? Never do this!
}

// ? DON'T: Plain text configuration
public class InsecureHandler : IAuthenticationHandler
{
    public InsecureHandler(IConfiguration config)
    {
        _token = config["ApiToken"];  // ? Not secure for production
    }
}
```

### ?? IHeaderHandler Security

**Your Responsibilities:**
- Header value sanitization (the framework handles standard headers automatically)
- Prevention of header injection attacks
- Protection of sensitive data in headers

```csharp
/// <summary>
/// SECURITY: Implementations must sanitize header values to prevent injection.
/// The framework provides SanitizeHttpHeaderValue() for this purpose.
/// </summary>
public class SecureHeaderHandler : IHeaderHandler
{
    public int ProcessingOrder => 0;

    public void SetHeader(HttpRequestMessage req)
    {
        var correlationId = Activity.Current?.Id ?? Guid.NewGuid().ToString();
        
        // ? Use only alphanumeric/dash characters in custom headers
        // ? Framework automatically sanitizes standard parameter headers
        req.Headers.Add("X-Correlation-Id", correlationId);
    }

    public void GetHeader(HttpResponseMessage response)
    {
        // ?? Don't log sensitive header values
        // ? Validate expected header formats before use
    }
}
```

**Header Injection Prevention:**
The framework automatically sanitizes header names and values per RFC 7230, removing:
- CR (`\r`) and LF (`\n`) characters
- URL-encoded variants (`%0d`, `%0a`)
- Double-encoded variants (`%250d`, `%250a`)
- Unicode line separators (`\u2028`, `\u2029`)

### ?? IErrorHandler Security

**Your Responsibilities:**
- Preventing sensitive information disclosure in error responses
- Logging errors securely (no credentials in logs)
- Providing meaningful but safe error messages to clients

```csharp
/// <summary>
/// SECURITY: Error handlers must not expose:
/// - Stack traces in production
/// - Internal system details
/// - Database connection strings
/// - File paths
/// - PII (emails, SSNs, etc.)
/// </summary>
public class SecureErrorHandler : IErrorHandler
{
    private readonly ILogger _logger;
    private readonly bool _isDevelopment;

    public SecureErrorHandler(ILogger logger, IHostEnvironment env)
    {
        _logger = logger;
        _isDevelopment = env.IsDevelopment();
    }

    public Exception ProduceClientException(
        string message, 
        HttpStatusCode statusCode, 
        Exception innerException, 
        string responseBody)
    {
        // ? Generate error ID for correlation
        var errorId = Guid.NewGuid().ToString("N");
        
        // ? Log full details internally
        _logger.LogError(innerException, "Error {ErrorId}: {Message}", errorId, message);
        
        // ? Return sanitized message to client
        var clientMessage = _isDevelopment 
            ? message 
            : $"An error occurred. Reference ID: {errorId}";
        
        return new ApiException(clientMessage, statusCode, errorId);
    }
}
```

### ?? IInputInterceptor Security

**Your Responsibilities:**
- Input validation before processing
- Protection against injection attacks (SQL, XSS, command injection)
- Rate limiting and request throttling

```csharp
/// <summary>
/// SECURITY: Input interceptors should validate:
/// - Required fields are present
/// - Data types and formats are correct
/// - Values are within acceptable ranges
/// - No injection attack patterns
/// </summary>
public class SecureInputValidator : IInputInterceptor
{
    public bool Intercept(object[] values, StateDictionary state, 
        out string message, out HttpStatusCode statusCode)
    {
        var errors = new List<string>();
        
        foreach (var value in values.Where(v => v != null))
        {
            // ? Use Data Annotations validation
            var context = new ValidationContext(value);
            var results = new List<ValidationResult>();
            
            if (!Validator.TryValidateObject(value, context, results, true))
            {
                errors.AddRange(results.Select(r => r.ErrorMessage));
            }
            
            // ? Additional security checks
            if (value is string str && ContainsInjectionPattern(str))
            {
                errors.Add("Invalid characters in input");
            }
        }
        
        if (errors.Any())
        {
            message = string.Join("; ", errors);
            statusCode = HttpStatusCode.BadRequest;
            return true;  // Cancel request
        }
        
        message = null;
        statusCode = HttpStatusCode.OK;
        return false;
    }

    private bool ContainsInjectionPattern(string input)
    {
        // ?? This is defense-in-depth only
        // ? Always use parameterized queries for database access
        return input.Contains("<script", StringComparison.OrdinalIgnoreCase) ||
               input.Contains("javascript:", StringComparison.OrdinalIgnoreCase);
    }
}
```

### ??? ISerializer Security (XML)

**Your Responsibilities:**
- XXE (XML External Entity) prevention
- Denial of service protection via size limits
- Schema validation

```csharp
/// <summary>
/// SECURITY: XML serializer implementations MUST:
/// - Disable DTD processing
/// - Disable external entity resolution
/// - Set maximum document size limits
/// </summary>
public class SecureXmlSerializer : ISerializer
{
    public string SerializationType => "xml";

    public object Deserialize(Stream stream, Type type)
    {
        // ? Secure XML reader settings
        var settings = new XmlReaderSettings
        {
            DtdProcessing = DtdProcessing.Prohibit,    // Prevent XXE
            XmlResolver = null,                         // No external entities
            MaxCharactersInDocument = 10_000_000,       // Size limit
            MaxCharactersFromEntities = 1000            // Entity expansion limit
        };

        using (var reader = XmlReader.Create(stream, settings))
        {
            var serializer = new XmlSerializer(type);
            return serializer.Deserialize(reader);
        }
    }

    // ... other members
}
```

### Security Implementation Checklist

Before deploying implementations of extensibility interfaces:

**IAuthenticationHandler:**
- [ ] Credentials retrieved from secure storage
- [ ] No hardcoded secrets
- [ ] Token refresh implemented
- [ ] Sensitive data cleared from memory

**IHeaderHandler:**
- [ ] User input sanitized before adding to headers
- [ ] No sensitive data in custom headers
- [ ] Correlation IDs use safe formats

**IErrorHandler:**
- [ ] Stack traces hidden in production
- [ ] No PII in error messages
- [ ] Error IDs generated for correlation
- [ ] Full errors logged internally

**IInputInterceptor:**
- [ ] Required field validation
- [ ] Type and format validation
- [ ] Injection pattern detection
- [ ] Size/length limits enforced

**ISerializer (XML):**
- [ ] DTD processing disabled
- [ ] External entities blocked
- [ ] Document size limits set

---

## Target Frameworks

.NET Standard 2.0 | .NET 6.0 | .NET 7.0 | .NET 8.0

## License

Apache-2.0

## Links

- [GitHub](https://github.com/JonasSyrstad/Stardust.Rest)
- [NuGet](https://www.nuget.org/packages/Stardust.Interstellar.Rest.Annotations)
- [Security Assessment](docs/SECURITY_ASSESSMENT.md)
