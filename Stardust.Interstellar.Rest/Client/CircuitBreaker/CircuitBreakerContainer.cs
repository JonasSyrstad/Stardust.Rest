using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Stardust.Interstellar.Rest.Annotations;
using Stardust.Interstellar.Rest.Extensions;

namespace Stardust.Interstellar.Rest.Client.CircuitBreaker
{
    public static class CircuitBreakerContainer
    {
        private static readonly ConcurrentDictionary<Type, CircuitBreakerServiceConfig> serviceConfigs = 
            new ConcurrentDictionary<Type, CircuitBreakerServiceConfig>();
        
        private static readonly ConcurrentDictionary<string, ICircuitBreaker> scopedBreakers = 
            new ConcurrentDictionary<string, ICircuitBreaker>();

        private static ICircuitBreakerIdentifierProvider identifierProvider = new DefaultCircuitBreakerIdentifierProvider();

        /// <summary>
        /// Sets a custom identifier provider for extracting user and client identifiers
        /// </summary>
        public static void SetIdentifierProvider(ICircuitBreakerIdentifierProvider provider)
        {
            identifierProvider = provider ?? throw new ArgumentNullException(nameof(provider));
        }

        internal static ICircuitBreaker GetCircuitBreaker(Type serviceType, IServiceProvider serviceProvider = null)
        {
            if (!serviceConfigs.TryGetValue(serviceType, out var config))
                return new NullBreaker();

            var scopeKey = GenerateScopeKey(serviceType, config.Attribute, serviceProvider);
            
            return scopedBreakers.GetOrAdd(scopeKey, key => 
            {
                // Check if we need to enforce instance limits
                if (config.Attribute.MaxInstancesPerService > 0)
                {
                    EnforceInstanceLimits(serviceType, config.Attribute.MaxInstancesPerService);
                }
                
                return new CircuitBreaker(config.Attribute, serviceProvider);
            });
        }

        internal static void Register(Type interfaceType, CircuitBreakerAttribute attribute, IServiceProvider serviceProvider = null)
        {
            var config = new CircuitBreakerServiceConfig
            {
                ServiceType = interfaceType,
                Attribute = attribute
            };
            
            serviceConfigs.TryAdd(interfaceType, config);

            // For common scope, we can pre-create the circuit breaker
            if (attribute.Scope == CircuitBreakerScope.Common)
            {
                var scopeKey = GenerateScopeKey(interfaceType, attribute, null);
                scopedBreakers.TryAdd(scopeKey, new CircuitBreaker(attribute, serviceProvider));
            }
        }

        internal static void Register(Type interfaceType, ICircuitBreaker circuitBreaker)
        {
            // Legacy registration method - treat as common scope
            var scopeKey = $"common:{interfaceType.FullName}";
            scopedBreakers.TryAdd(scopeKey, circuitBreaker);
        }

        public static T ExecuteWithCircuitBreaker<TExtDep, T>(this TExtDep externaDependency, string path, Func<TExtDep, T> func, IServiceProvider provider)
        {
            return GetCircuitBreaker(externaDependency.GetType(), provider).Invoke(GetActionUrl<TExtDep>(path), () => func(externaDependency), provider);
        }

        private static string GetActionUrl<TExtDep>(string path)
        {
            return string.IsNullOrEmpty(path) ? typeof(TExtDep).FullName : path;
        }

        public static async Task<T> ExecuteWithCircuitBreakerAsync<TExtDep, T>(this TExtDep externaDependency, string path, Func<TExtDep, Task<T>> func, IServiceProvider provider)
        {
            return await GetCircuitBreaker(externaDependency.GetType(), provider).InvokeAsync(GetActionUrl<TExtDep>(path), async () => await func(externaDependency), provider);
        }

        public static void Register<T>(int threshold, int timeout, IServiceLocator serviceLocator)
        {
            Register(typeof(T), new CircuitBreakerAttribute(threshold, timeout), serviceLocator);
        }

        public static async Task ExecuteWithCircuitBreakerAsync<TExtDep>(this TExtDep externaDependency, string path, Func<TExtDep, Task> func, IServiceProvider provider)
        {
            await GetCircuitBreaker(externaDependency.GetType(), provider).InvokeAsync(GetActionUrl<TExtDep>(path), async () => await func(externaDependency), provider);
        }

        public static void ExecuteWithCircuitBreaker<TExtDep>(this TExtDep externaDependency, string path, Action<TExtDep> func, IServiceProvider provider)
        {
            GetCircuitBreaker(externaDependency.GetType(), provider).Invoke(GetActionUrl<TExtDep>(path), () => func(externaDependency), provider);
        }

        public static KeyValuePair<int, TimeSpan?>? GetCircuitState<T>(this T circuit, IServiceProvider serviceProvider = null)
        {
            var state = GetCircuitBreaker(typeof(T), serviceProvider) as ICircuit;
            if (state == null) return null;
            return new KeyValuePair<int, TimeSpan?>(state.Failures, state.SuspendedTime);
        }

        /// <summary>
        /// Gets circuit breaker statistics for all scoped instances of a service type
        /// </summary>
        public static Dictionary<string, KeyValuePair<int, TimeSpan?>> GetAllCircuitStates<T>()
        {
            var result = new Dictionary<string, KeyValuePair<int, TimeSpan?>>();
            var serviceTypePrefix = typeof(T).FullName;

            foreach (var kvp in scopedBreakers)
            {
                if (kvp.Key.Contains(serviceTypePrefix) && kvp.Value is ICircuit circuit)
                {
                    result[kvp.Key] = new KeyValuePair<int, TimeSpan?>(circuit.Failures, circuit.SuspendedTime);
                }
            }

            return result;
        }

        /// <summary>
        /// Clears all circuit breaker instances (useful for testing or cleanup)
        /// </summary>
        public static void ClearAll()
        {
            scopedBreakers.Clear();
            serviceConfigs.Clear();
        }

        /// <summary>
        /// Clears circuit breaker instances for a specific service type
        /// </summary>
        public static void Clear<T>()
        {
            var serviceTypePrefix = typeof(T).FullName;
            var keysToRemove = new List<string>();

            foreach (var kvp in scopedBreakers)
            {
                if (kvp.Key.Contains(serviceTypePrefix))
                {
                    keysToRemove.Add(kvp.Key);
                }
            }

            foreach (var key in keysToRemove)
            {
                scopedBreakers.TryRemove(key, out _);
            }

            serviceConfigs.TryRemove(typeof(T), out _);
        }

        private static string GenerateScopeKey(Type serviceType, CircuitBreakerAttribute attribute, IServiceProvider serviceProvider)
        {
            var baseKey = serviceType.FullName;
            
            if (attribute.Scope == CircuitBreakerScope.Common)
            {
                return $"common:{baseKey}";
            }

            var keyParts = new List<string> { baseKey };

            if (attribute.Scope == CircuitBreakerScope.User || attribute.Scope == CircuitBreakerScope.UserAndClient)
            {
                var userId = identifierProvider?.GetUserIdentifier(serviceProvider, attribute.UserIdentifierProperty);
                if (!string.IsNullOrEmpty(userId))
                {
                    if (!attribute.CaseSensitiveIdentifiers)
                        userId = userId.ToLowerInvariant();
                    keyParts.Add($"user:{userId}");
                }
                else
                {
                    // Fallback to common if user ID not available
                    return $"common:{baseKey}";
                }
            }

            if (attribute.Scope == CircuitBreakerScope.Client || attribute.Scope == CircuitBreakerScope.UserAndClient)
            {
                var clientId = identifierProvider?.GetClientIdentifier(serviceProvider, attribute.ClientIdentifierProperty);
                if (!string.IsNullOrEmpty(clientId))
                {
                    if (!attribute.CaseSensitiveIdentifiers)
                        clientId = clientId.ToLowerInvariant();
                    keyParts.Add($"client:{clientId}");
                }
                else if (attribute.Scope == CircuitBreakerScope.Client)
                {
                    // Fallback to common if client ID not available and it's client-only scope
                    return $"common:{baseKey}";
                }
                // For UserAndClient scope, missing client ID is OK if we have user ID
            }

            return string.Join("|", keyParts);
        }

        private static void EnforceInstanceLimits(Type serviceType, int maxInstances)
        {
            var serviceTypePrefix = serviceType.FullName;
            var serviceInstances = new List<KeyValuePair<string, ICircuitBreaker>>();

            foreach (var kvp in scopedBreakers)
            {
                if (kvp.Key.Contains(serviceTypePrefix))
                {
                    serviceInstances.Add(kvp);
                }
            }

            if (serviceInstances.Count >= maxInstances)
            {
                // Remove oldest instances (this is a simple LRU-like approach)
                // In a more sophisticated implementation, you might track access times
                var instancesToRemove = serviceInstances.Count - maxInstances + 1;
                for (int i = 0; i < instancesToRemove; i++)
                {
                    scopedBreakers.TryRemove(serviceInstances[i].Key, out _);
                }
            }
        }

        private class CircuitBreakerServiceConfig
        {
            public Type ServiceType { get; set; }
            public CircuitBreakerAttribute Attribute { get; set; }
        }
    }
}
