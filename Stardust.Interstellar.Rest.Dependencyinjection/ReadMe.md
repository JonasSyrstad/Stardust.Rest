# Stardust.Interstellar.Rest.DependencyInjection

Dependency injection support for .NET Framework 4.x applications using Stardust.Rest. This package enables the dynamic controller generation and client proxy features in OWIN-based ASP.NET applications.

## Part of the Stardust.Rest Ecosystem

This package is part of **Stardust.Rest**, a comprehensive solution for contract-first REST API development in .NET. The ecosystem enables you to define your API contracts once as decorated C# interfaces and automatically generate both client proxies and server implementations.

### Why Stardust.Rest?

**🎯 Contract-First Development**
- Define your API contract as a C# interface with attributes
- Share the same interface between client and server projects
- Ensure perfect alignment between your API consumers and providers

**⚡ Zero Boilerplate**
- No manual `HttpClient` code for clients
- No manual controller scaffolding for servers
- Generated code handles routing, serialization, error handling, and more

**🛡️ Built-in Resilience**
- Circuit breaker pattern to prevent cascading failures
- Configurable retry policies with exponential backoff
- Custom error categorization for transient failure handling

**🔧 Highly Extensible**
- Pluggable authentication handlers
- Custom header processing pipelines
- Custom serialization support (JSON/XML)
- Custom error handlers

### Ecosystem Packages

| Package | Purpose |
|---------|---------|
| **Stardust.Interstellar.Rest.Annotations** | Core attributes for defining API contracts |
| **Stardust.Interstellar.Rest** | Client proxy generation - creates HTTP clients from interfaces |
| **Stardust.Interstellar.Rest.Service.AspNet** | Server generation for .NET Framework (Web API) |
| **Stardust.Interstellar.Rest.DependencyInjection** | DI support for .NET Framework OWIN applications (this package) |

---

## Installation

```bash
Install-Package Stardust.Interstellar.Rest.DependencyInjection
```

## Quick Start

### 1. Configure OWIN Startup

In your `Startup.cs`:

```csharp
public class Startup
{
    public void Configuration(IAppBuilder app)
    {
        // Add dependency injection with controller types
        // ControllerTypes.Both = MVC + WebAPI controllers
        app.AddDependencyInjection<Services>(ControllerTypes.Both);
        
        // Or choose specific controller types:
        // app.AddDependencyInjection<Services>(ControllerTypes.Mvc);
        // app.AddDependencyInjection<Services>(ControllerTypes.WebApi);
    }
}
```

### 2. Create Services Configuration

Create a new class that inherits from `ServicesConfiguration`:

```csharp
public class Services : ServicesConfiguration
{
    protected override IServiceCollection Configure(IServiceCollection services)
    {
        // Add Interstellar WebAPI controller generation support
        services.AddInterstellarServices();
        
        // Register REST API clients
        services.AddScoped(s => s.CreateRestClient<IExternalService>("https://api.example.com/"));
        
        // Create and bind dynamic WebAPI controllers from interfaces
        services.AddAsController<IUserService, UserService>(services.BuildServiceProvider());
        services.AddAsController<IProductService, ProductService>(services.BuildServiceProvider());
        
        // Finalize controller registration (MUST be called after all AddAsController calls)
        services.FinalizeRegistration();
        
        // Add additional service registrations
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IProductRepository, ProductRepository>();
        
        return services;
    }
}
```

### 3. Define Service Interfaces

```csharp
using Stardust.Interstellar.Rest.Annotations;

[Api("api/users")]
public interface IUserService
{
    [Get("{id}")]
    Task<User> GetUserAsync([InPath] string id);

    [Get]
    Task<IEnumerable<User>> GetAllUsersAsync();

    [Post]
    Task<User> CreateUserAsync([InBody] User user);
}
```

### 4. Implement Service

```csharp
public class UserService : IUserService
{
    private readonly IUserRepository _repository;

    public UserService(IUserRepository repository)
    {
        _repository = repository;
    }

    public async Task<User> GetUserAsync(string id)
        => await _repository.GetByIdAsync(id);

    public async Task<IEnumerable<User>> GetAllUsersAsync()
        => await _repository.GetAllAsync();

    public async Task<User> CreateUserAsync(User user)
        => await _repository.CreateAsync(user);
}
```

## Target Framework

- .NET Framework 4.7.1

## Dependencies

- Stardust.Interstellar.Rest.Annotations
- Stardust.Interstellar.Rest.Service
- Microsoft.Extensions.DependencyInjection

## License

Apache-2.0

## Links

- [GitHub Repository](https://github.com/JonasSyrstad/Stardust.Rest)
- [NuGet Package](https://www.nuget.org/packages/Stardust.Interstellar.Rest.DependencyInjection)