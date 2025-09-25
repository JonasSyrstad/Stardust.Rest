using System;
using System.Net;
using System.Threading.Tasks;
using Stardust.Interstellar.Rest.Annotations;

namespace Examples
{
    /// <summary>
    /// Example demonstrating different circuit breaker scoping options
    /// </summary>
    public static class CircuitBreakerScopingExamples
    {
        // Example 1: Common scope (default) - all users and clients share the same circuit breaker
        [CircuitBreaker(threshold: 5, timeoutInMinutes: 2)]
        public interface ICommonScopeService
        {
            Task<string> GetDataAsync();
        }

        // Example 2: User scope - each user gets their own circuit breaker instance
        [CircuitBreaker(threshold: 3, timeoutInMinutes: 1, resetTimeout: 5, CircuitBreakerScope.User)]
        public interface IUserScopeService
        {
            Task<string> GetUserDataAsync();
        }

        // Example 3: Client scope - each client application gets their own circuit breaker
        [CircuitBreaker(threshold: 10, timeoutInMinutes: 3, resetTimeout: 10, CircuitBreakerScope.Client)]
        public interface IClientScopeService
        {
            Task<string> GetClientDataAsync();
        }

        // Example 4: User + Client scope - each user+client combination gets their own circuit breaker
        [CircuitBreaker(threshold: 5, timeoutInMinutes: 2, resetTimeout: 5, CircuitBreakerScope.UserAndClient)]
        public interface IUserClientScopeService
        {
            Task<string> GetCombinedDataAsync();
        }

        // Example 5: Custom identifier properties for user/client extraction
        [CircuitBreaker(
            threshold: 5, 
            timeoutInMinutes: 2, 
            resetTimeout: 5, 
            CircuitBreakerScope.UserAndClient,
            userIdentifierProperty: "employee_id",     // Custom claim for user identification
            clientIdentifierProperty: "application_id"  // Custom claim for client identification
        )]
        public interface ICustomIdentifierService
        {
            Task<string> GetCustomDataAsync();
        }

        // Example 6: Advanced configuration with ignored exceptions and status codes
        [CircuitBreaker(
            threshold: 3,
            timeoutInMinutes: 1,
            resetTimeout: 2,
            CircuitBreakerScope.User,
            new[] { typeof(ArgumentException), typeof(InvalidOperationException) }, // Ignored exception types
            new[] { HttpStatusCode.BadRequest, HttpStatusCode.Forbidden }           // Ignored HTTP status codes
        )]
        public interface IAdvancedScopeService
        {
            Task<string> GetAdvancedDataAsync();
        }

        // Example 7: Configuration with instance limits to prevent memory leaks
        [CircuitBreaker(
            threshold: 5,
            timeoutInMinutes: 2,
            resetTimeout: 5,
            CircuitBreakerScope.UserAndClient,
            MaxInstancesPerService = 500,        // Limit to 500 circuit breaker instances per service
            CaseSensitiveIdentifiers = false     // Case-insensitive user/client matching
        )]
        public interface ILimitedInstanceService
        {
            Task<string> GetLimitedDataAsync();
        }
    }

    /// <summary>
    /// Custom identifier provider example for extracting user/client information
    /// from custom contexts (e.g., custom headers, database lookups, etc.)
    /// </summary>
    public class CustomCircuitBreakerIdentifierProvider : DefaultCircuitBreakerIdentifierProvider
    {
        public override string GetUserIdentifier(IServiceProvider serviceProvider, string userIdentifierProperty = null)
        {
            // Try custom logic first
            try
            {
                // Example: Extract from custom header
                var httpContextAccessor = serviceProvider?.GetService(Type.GetType("Microsoft.AspNetCore.Http.IHttpContextAccessor, Microsoft.AspNetCore.Http.Abstractions"));
                if (httpContextAccessor != null)
                {
                    var httpContext = httpContextAccessor.GetType().GetProperty("HttpContext")?.GetValue(httpContextAccessor);
                    if (httpContext != null)
                    {
                        var request = httpContext.GetType().GetProperty("Request")?.GetValue(httpContext);
                        if (request != null)
                        {
                            var headers = request.GetType().GetProperty("Headers")?.GetValue(request);
                            if (headers != null)
                            {
                                // Try to get custom user identifier from header
                                var customUserHeader = headers.GetType().GetProperty("Item")?.GetValue(headers, new object[] { "X-Custom-User-Id" });
                                if (customUserHeader != null)
                                {
                                    return customUserHeader.ToString();
                                }
                            }
                        }
                    }
                }
            }
            catch
            {
                // Fall back to default implementation if custom logic fails
            }

            // Fall back to base implementation (claims-based)
            return base.GetUserIdentifier(serviceProvider, userIdentifierProperty);
        }

        public override string GetClientIdentifier(IServiceProvider serviceProvider, string clientIdentifierProperty = null)
        {
            // Similar custom logic for client identification
            // Could extract from API keys, custom headers, certificates, etc.
            return base.GetClientIdentifier(serviceProvider, clientIdentifierProperty);
        }
    }

    /// <summary>
    /// Example of how to configure and use the scoped circuit breaker
    /// </summary>
    public class CircuitBreakerUsageExample
    {
        public void ConfigureServices(IServiceProvider serviceProvider)
        {
            // Optional: Set custom identifier provider
            CircuitBreakerContainer.SetIdentifierProvider(new CustomCircuitBreakerIdentifierProvider());
        }

        public void DemonstrateUsage()
        {
            // Get circuit breaker state for monitoring
            var circuitState = ((IUserScopeService)null).GetCircuitState();
            if (circuitState.HasValue)
            {
                Console.WriteLine($"Circuit breaker failures: {circuitState.Value.Key}");
                Console.WriteLine($"Suspended time: {circuitState.Value.Value}");
            }

            // Get all circuit states for a service (useful for monitoring dashboards)
            var allStates = CircuitBreakerContainer.GetAllCircuitStates<IUserScopeService>();
            foreach (var state in allStates)
            {
                Console.WriteLine($"Scope: {state.Key}, Failures: {state.Value.Key}, Suspended: {state.Value.Value}");
            }

            // Clear circuit breakers for testing/cleanup
            CircuitBreakerContainer.Clear<IUserScopeService>();
            // or clear all
            CircuitBreakerContainer.ClearAll();
        }
    }

    /// <summary>
    /// Scoping behavior documentation:
    /// 
    /// 1. Common Scope (Default):
    ///    - All calls to the service share the same circuit breaker instance
    ///    - Fastest performance, lowest memory usage
    ///    - Use when you want global circuit breaking behavior
    /// 
    /// 2. User Scope:
    ///    - Each authenticated user gets their own circuit breaker
    ///    - Useful when you want to isolate failures per user
    ///    - Falls back to Common scope if user identification fails
    /// 
    /// 3. Client Scope:
    ///    - Each client application gets their own circuit breaker
    ///    - Useful in multi-tenant scenarios or API gateway patterns
    ///    - Falls back to Common scope if client identification fails
    /// 
    /// 4. UserAndClient Scope:
    ///    - Each user+client combination gets their own circuit breaker
    ///    - Highest isolation but potentially highest memory usage
    ///    - Falls back to User scope if client ID unavailable, or Common if both unavailable
    /// 
    /// Memory Management:
    /// - MaxInstancesPerService limits prevent memory leaks in high-traffic scenarios
    /// - Oldest instances are evicted when limits are reached (simple LRU)
    /// - Consider monitoring and tuning based on your traffic patterns
    /// 
    /// Identifier Resolution:
    /// - Uses standard JWT claims by default (sub, client_id, etc.)
    /// - Custom properties allow specifying exact claim names
    /// - Custom identifier providers enable complex extraction logic
    /// - Case sensitivity is configurable per service
    /// </summary>
    public class CircuitBreakerScopingDocumentation
    {
        // This class serves as documentation - see summary above
    }
}