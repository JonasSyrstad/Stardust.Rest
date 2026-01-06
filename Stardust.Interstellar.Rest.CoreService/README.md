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

## Full Example

```csharp
// Program.cs (.NET 6+)
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddInterstellar();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IProductService, ProductService>();

builder.Services.AddMvc()
    .AddAsController<IUserService, UserService>()
    .UseInterstellar();

builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseRouting();
app.UseAuthorization();

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
