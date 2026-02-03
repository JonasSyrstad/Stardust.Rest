# Stardust.Interstellar.Rest.Service.AspNetCore

> **Define once. Generate everywhere.**  
> Generate ASP.NET Core controllers and endpoints from decorated interfaces.

[![NuGet](https://img.shields.io/nuget/v/Stardust.Interstellar.Rest.Service.AspNetCore.svg)](https://www.nuget.org/packages/Stardust.Interstellar.Rest.Service.AspNetCore)

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
| **[Client](https://www.nuget.org/packages/Stardust.Interstellar.Rest)** | Generate HTTP clients from interfaces |
| **[Server](https://www.nuget.org/packages/Stardust.Interstellar.Rest.Service.AspNetCore)** | Generate ASP.NET Core controllers from interfaces ← *you are here* |

### Why use it?

- 🎯 **Contract-first** — One interface = perfect client/server alignment
- ⚡ **Zero boilerplate** — No manual HTTP code or controller scaffolding
- 🛡️ **Built-in resilience** — Circuit breakers, retries, error handling
- 🔌 **Extensible** — Custom auth, headers, serialization, error handlers

---

## Installation

```bash
dotnet add package Stardust.Interstellar.Rest.Service.AspNetCore
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
}
```

### 2. Implement the service

```csharp
public class UserService : IUserService
{
    private readonly IUserRepository _repo;
    public UserService(IUserRepository repo) => _repo = repo;

    public Task<User> GetUserAsync(string id) => _repo.GetByIdAsync(id);
    public Task<IEnumerable<User>> GetAllAsync() => _repo.GetAllAsync();
    public Task<User> CreateAsync(User user) => _repo.CreateAsync(user);
}
```

### 3. Register as controller

```csharp
services.AddInterstellar();

services.AddMvc()
    .AddAsController<IUserService, UserService>()
    .UseInterstellar();  // Don't forget this!
```

### 4. Map routes

```csharp
app.UseRouting();
app.UseEndpoints(endpoints => endpoints.MapControllers());
```

---

## Registration Options

### MVC Controllers

```csharp
services.AddMvc()
    // With implementation type
    .AddAsController<IUserService, UserService>()
    // Or if already in DI
    .AddAsController<IProductService>()
    // Or with factory
    .AddAsController<IOrderService>(sp => new OrderService(sp.GetRequiredService<IOrderRepo>()))
    // Always call UseInterstellar() at the end
    .UseInterstellar();
```

### Minimal API Endpoints (.NET 6+)

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddInterstellar();
builder.Services.AddScoped<IUserService, UserService>();

var app = builder.Build();

app.UseRouting();
app.UseEndpoints(endpoints =>
{
    endpoints.AddAsEndpoints<IUserService>();
});

app.Run();
```

### Proxy Controller

Forward requests to another service:

```csharp
services.AddMvc()
    .AddProxyController<IExternalService>("https://api.external.com")
    .UseInterstellar();
```

---

## Parameter Binding

Parameters are automatically bound with proper ASP.NET Core attributes:

```csharp
[Get("search")]
Task<SearchResult> SearchAsync(
    [InQuery("q")] string searchQuery,           // [FromQuery(Name = "q")]
    [InQuery("page_size")] int pageSize,         // [FromQuery(Name = "page_size")]
    [InHeader("X-Api-Key")] string apiKey);      // [FromHeader(Name = "X-Api-Key")]

[Post]
Task<User> CreateAsync([InBody] User user);      // [FromBody]

[Get("users/{id}")]
Task<User> GetAsync([InPath] string id);         // [FromRoute]
```

---

## Authorization

### Interface-level

```csharp
[Api("api/admin")]
[AuthorizeWrapper(Roles = "Admin")]
public interface IAdminService { }
```

### Method-level

```csharp
[Api("api/users")]
public interface IUserService
{
    [Get]
    Task<IEnumerable<User>> GetAllAsync();

    [Delete("{id}")]
    [AuthorizeWrapper(Policy = "CanDelete")]
    Task DeleteAsync([InPath] string id);
}
```

---

## Response Status Codes

```csharp
[Post]
[SuccessStatusCode(HttpStatusCode.Created)]
Task<User> CreateAsync([InBody] User user);

[Delete("{id}")]
[SuccessStatusCode(HttpStatusCode.NoContent)]
Task DeleteAsync([InPath] string id);
```

---

## API Versioning

```csharp
[Api("api/users")]
[Version("1.0")]
public interface IUserServiceV1 { }

[Api("api/users")]
[Version("2.0")]
public interface IUserServiceV2 { }
```

Configure with Microsoft.AspNetCore.Mvc.Versioning:

```csharp
services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
});

services.AddMvc()
    .AddAsController<IUserServiceV1, UserServiceV1>()
    .AddAsController<IUserServiceV2, UserServiceV2>()
    .UseInterstellar();
```

---

## OpenAPI / Swagger

Generated controllers work with Swashbuckle out of the box:

```csharp
[Api("api/users")]
[ServiceDescription("User management", Tags = "Users")]
public interface IUserService
{
    [Get("{id}", "Get user by ID")]
    Task<User> GetAsync([InPath(Description = "Unique user ID")] string id);

    [Get("search", "Search users")]
    Task<List<User>> SearchAsync(
        [InQuery("q", Description = "Search query")] string query,
        [InQuery("page_size", Description = "Results per page")] int pageSize);
}
```

---

## Endpoint Routing Policies (.NET 6+)

Apply custom policies to minimal API endpoints:

```csharp
public class CachingPolicy : IEndpointRoutingPolicy
{
    public string PolicyName => "Caching";
    public void ApplyPolicyAsync(RouteHandlerBuilder builder) { /* ... */ }
    public Dictionary<string, string> GetResponseHeaders() 
        => new() { ["Cache-Control"] = "public, max-age=300" };
}

// Register
services.AddSingleton<IEndpointRoutingPolicy>(new CachingPolicy());

// Apply
[Api("api/items")]
[EndpointRoutingPolicy("SecurityHeaders")]  // All endpoints
public interface IItemService
{
    [Get]
    [EndpointRoutingPolicy("Caching")]      // This endpoint only
    Task<List<Item>> GetItemsAsync();
}
```

---

## Service Initialization

Perform setup when the service is instantiated:

```csharp
public class UserService : IUserService, IInitializableService
{
    public void Initialize(StateDictionary state, object[] parameters)
    {
        // Called before method execution
    }
}
```

---

## Contract-First Architecture

Share interfaces between client and server:

```
MyApi.Contracts/           ← Shared library
├── IUserService.cs
├── IProductService.cs
└── Models/
    └── User.cs

MyApi.Server/              ← Uses this package
├── References MyApi.Contracts
└── Implements services

MyApi.Client/              ← Uses Stardust.Interstellar.Rest
├── References MyApi.Contracts
└── Consumes services via proxy
```

---

## Configuration

```csharp
// Debug mode - throws during generation
ServiceFactory.ThrowOnException = true;

// Disable version header
ServiceFactory.DisableStardustVersion();
```

---

## 🔒 Security Considerations

The server library generates ASP.NET Core controllers and endpoints from your interfaces. Several security aspects require developer attention.

### Built-in Security Features

| Feature | Status | Description |
|---------|--------|-------------|
| HTTP Header Sanitization | ✅ Built-in | Response headers are automatically sanitized |
| ASP.NET Core Integration | ✅ Built-in | Leverages ASP.NET Core's security infrastructure |
| Authorization Support | ✅ Built-in | `[AuthorizeWrapper]` maps to ASP.NET Core authorization |
| Model Binding | ✅ Built-in | Uses ASP.NET Core's secure model binding |

### Developer Responsibilities

#### 🚨 Error Handling & Information Disclosure

By default, exception messages may be exposed to clients. Implement a secure error handler:

```csharp
public class SecureErrorHandler : IErrorHandler
{
    private readonly ILogger<SecureErrorHandler> _logger;
    private readonly IHostEnvironment _env;

    public SecureErrorHandler(ILogger<SecureErrorHandler> logger, IHostEnvironment env)
    {
        _logger = logger;
        _env = env;
    }

    public HttpResponseMessage ConvertToErrorResponse(Exception ex, HttpRequestMessage req)
    {
        // ✅ Generate unique error ID for correlation
        var errorId = Guid.NewGuid().ToString("N");
        
        // ✅ Log full exception internally
        _logger.LogError(ex, "Error {ErrorId} processing {Method} {Path}", 
            errorId, req.Method, req.RequestUri?.PathAndQuery);
        
        // ✅ Return sanitized response based on environment
        var response = new ErrorResponse
        {
            ErrorId = errorId,
            Message = _env.IsDevelopment() 
                ? ex.Message 
                : "An error occurred processing your request.",
            // ❌ Never include: ex.StackTrace, ex.InnerException, connection strings
        };
        
        return req.CreateResponse(GetStatusCode(ex), response);
    }

    private HttpStatusCode GetStatusCode(Exception ex) => ex switch
    {
        ArgumentException => HttpStatusCode.BadRequest,
        UnauthorizedAccessException => HttpStatusCode.Unauthorized,
        KeyNotFoundException => HttpStatusCode.NotFound,
        _ => HttpStatusCode.InternalServerError
    };
}

// Register globally
services.AddScoped<IErrorHandler, SecureErrorHandler>();
```

**⚠️ Information Disclosure Risks:**
- ❌ Stack traces reveal internal structure
- ❌ Exception messages may contain file paths
- ❌ Database errors may expose connection strings
- ❌ Validation errors may reveal expected formats

#### 🔐 Authorization Best Practices

Use ASP.NET Core's authorization system with `[AuthorizeWrapper]`:

```csharp
[Api("api/admin")]
[AuthorizeWrapper(Roles = "Admin")]  // Require Admin role for all methods
public interface IAdminService
{
    [Get("users")]
    Task<List<User>> GetAllUsersAsync();

    [Delete("users/{id}")]
    [AuthorizeWrapper(Policy = "SuperAdmin")]  // Additional policy requirement
    Task DeleteUserAsync([InPath] string id);
}
```

**Configure authorization:**
```csharp
services.AddAuthorization(options =>
{
    options.AddPolicy("SuperAdmin", policy =>
        policy.RequireRole("Admin")
              .RequireClaim("department", "IT"));
});

// Require authentication by default
services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});
```

#### 🛡️ Input Validation

Implement input validation using `IInputInterceptor` or ASP.NET Core model validation:

```csharp
// Option 1: Use Data Annotations on your models
public class CreateUserRequest
{
    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string Name { get; set; }

    [Required]
    [EmailAddress]
    public string Email { get; set; }

    [Range(0, 150)]
    public int? Age { get; set; }
}

// Option 2: Custom input interceptor
public class ValidationInterceptor : IInputInterceptor
{
    public bool Intercept(object[] values, StateDictionary state,
        out string message, out HttpStatusCode statusCode)
    {
        foreach (var value in values.Where(v => v != null))
        {
            var context = new ValidationContext(value);
            var results = new List<ValidationResult>();
            
            if (!Validator.TryValidateObject(value, context, results, true))
            {
                message = string.Join("; ", results.Select(r => r.ErrorMessage));
                statusCode = HttpStatusCode.BadRequest;
                return true;  // Cancel request
            }
        }
        
        message = null;
        statusCode = HttpStatusCode.OK;
        return false;
    }
}

// Apply to interface
[InputInterceptor(typeof(ValidationInterceptor))]
public interface IUserService { }
```

#### ⏱️ Rate Limiting

Protect against DoS attacks using the built-in throttling:

```csharp
// Interface-level rate limiting
[Api("api/users")]
[Throttling(maxRequestsPerSecound: 100)]
public interface IUserService { }

// Method-level (more restrictive for expensive operations)
[Api("api/reports")]
public interface IReportService
{
    [Get]
    [Throttling(maxRequestsPerSecound: 10)]  // Expensive operation
    Task<Report> GenerateReportAsync([InQuery] ReportRequest request);
}
```

**⚠️ Limitations:**
- Built-in throttling is in-memory only
- For distributed environments, use ASP.NET Core Rate Limiting or a custom `IThrottlingManager`

```csharp
// .NET 7+ Rate Limiting Middleware (recommended for production)
builder.Services.AddRateLimiter(options =>
{
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.User.Identity?.Name ?? context.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                Window = TimeSpan.FromSeconds(1),
                PermitLimit = 100
            }));
});

app.UseRateLimiter();
```

#### 📋 Response Header Security

Add security headers using endpoint routing policies:

```csharp
public class SecurityHeadersPolicy : IEndpointRoutingPolicy
{
    public string PolicyName => "SecurityHeaders";

    public void ApplyPolicyAsync(RouteHandlerBuilder builder)
    {
        builder.AddEndpointFilter(async (context, next) =>
        {
            var result = await next(context);
            
            var response = context.HttpContext.Response;
            response.Headers["X-Content-Type-Options"] = "nosniff";
            response.Headers["X-Frame-Options"] = "DENY";
            response.Headers["X-XSS-Protection"] = "1; mode=block";
            response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
            
            return result;
        });
    }

    public Dictionary<string, string> GetResponseHeaders() => new()
    {
        ["X-Content-Type-Options"] = "nosniff",
        ["X-Frame-Options"] = "DENY"
    };
}

// Apply to all endpoints
[Api("api/users")]
[EndpointRoutingPolicy("SecurityHeaders")]
public interface IUserService { }
```

#### 🔍 Logging & Auditing

Implement proper logging without exposing sensitive data:

```csharp
public class AuditingHeaderHandler : IHeaderHandler
{
    private readonly ILogger _logger;
    
    public int ProcessingOrder => int.MaxValue;  // Run last

    public void SetHeader(HttpRequestMessage req)
    {
        // Add correlation ID for tracing
        var correlationId = Activity.Current?.Id ?? Guid.NewGuid().ToString();
        req.Headers.Add("X-Correlation-Id", correlationId);
    }

    public void GetHeader(HttpResponseMessage response)
    {
        // ✅ Log request completion
        // ❌ Don't log: Authorization headers, request bodies with PII
        _logger.LogInformation(
            "Request completed: {Method} {Path} -> {StatusCode}",
            response.RequestMessage?.Method,
            response.RequestMessage?.RequestUri?.PathAndQuery,
            response.StatusCode);
    }
}
```

### Security Configuration Checklist

**Error Handling:**
- [ ] Custom `IErrorHandler` implemented
- [ ] Stack traces hidden in production
- [ ] Error IDs generated for correlation
- [ ] Sensitive data scrubbed from messages

**Authorization:**
- [ ] `[AuthorizeWrapper]` applied appropriately
- [ ] Policies configured for sensitive operations
- [ ] Default deny policy considered

**Input Validation:**
- [ ] Data Annotations on request models
- [ ] Custom `IInputInterceptor` for complex validation
- [ ] Size limits on request bodies

**Rate Limiting:**
- [ ] `[Throttling]` applied to public endpoints
- [ ] Distributed rate limiting for scaled deployments
- [ ] Different limits for different operations

**Response Security:**
- [ ] Security headers added
- [ ] CORS configured appropriately
- [ ] Sensitive data not in responses

---

## Full Example

```csharp
// Program.cs (.NET 6+)
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddInterstellar();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IProductService, ProductService>();

// Security: Add error handler
builder.Services.AddScoped<IErrorHandler, SecureErrorHandler>();

builder.Services.AddMvc()
    .AddAsController<IUserService, UserService>()
    .UseInterstellar();

builder.Services.AddSwaggerGen();

// Security: Add authorization
builder.Services.AddAuthorization();

// Security: Add rate limiting (.NET 7+)
builder.Services.AddRateLimiter(options => { /* ... */ });

var app = builder.Build();

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

app.UseEndpoints(endpoints =>
{
    endpoints.MapControllers();
    endpoints.AddAsEndpoints<IProductService>();
});

app.UseSwagger();
app.UseSwaggerUI();

app.Run();
```

---

## Target Frameworks

.NET Standard 2.0 | .NET Core 3.1 | .NET 5.0 | .NET 6.0 | .NET 7.0 | .NET 8.0

## Dependencies

- Stardust.Interstellar.Rest.Annotations
- Stardust.Interstellar.Rest
- Microsoft.AspNetCore.Mvc.Versioning

## License

Apache-2.0

## Links

- [GitHub](https://github.com/JonasSyrstad/Stardust.Rest)
- [NuGet](https://www.nuget.org/packages/Stardust.Interstellar.Rest.Service.AspNetCore)
- [Security Assessment](docs/SECURITY_ASSESSMENT.md)
