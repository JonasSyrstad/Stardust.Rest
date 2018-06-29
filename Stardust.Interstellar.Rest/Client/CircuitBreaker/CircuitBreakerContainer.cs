using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Stardust.Interstellar.Rest.Annotations;
using Stardust.Interstellar.Rest.Common;
using Stardust.Interstellar.Rest.Extensions;

namespace Stardust.Interstellar.Rest.Client.CircuitBreaker
{
    public static class CircuitBreakerContainer
    {
        private static ConcurrentDictionary<Type, ICircuitBreaker> breakers = new ConcurrentDictionary<Type, ICircuitBreaker>();
        internal static ICircuitBreaker GetCircuitBreaker(Type serviceType,IServiceLocator locator=null)
        {
            ICircuitBreaker breaker;
            if (breakers.TryGetValue(serviceType, out breaker)) return breaker;
            return new NullBreaker(locator);
        }

        internal static void Register(Type interfaceType, ICircuitBreaker circuitBreaker)
        {
            breakers.TryAdd(interfaceType, circuitBreaker);
        }

        public static T ExecuteWithCircuitBreaker<TExtDep, T>(this TExtDep externaDependency, string path, Func<TExtDep, T> func)
        {
            return GetCircuitBreaker(externaDependency.GetType()).Invoke(GetActionUrl<TExtDep>(path), () => func(externaDependency));
        }

        private static string GetActionUrl<TExtDep>(string path)
        {
            return string.IsNullOrEmpty(path) ? typeof(TExtDep).FullName : path;
        }

        public static async Task<T> ExecuteWithCircuitBreakerAsync<TExtDep, T>(this TExtDep externaDependency, string path, Func<TExtDep, Task<T>> func)
        {
            return await GetCircuitBreaker(externaDependency.GetType()).InvokeAsync(GetActionUrl<TExtDep>(path), async () => await func(externaDependency));
        }

        public static void Register<T>(int threshold, int timeout, IServiceLocator serviceLocator)
        {
            Register(typeof(T), new CircuitBreaker(new CircuitBreakerAttribute(threshold, timeout), serviceLocator));
        }

        public static async Task ExecuteWithCircuitBreakerAsync<TExtDep>(this TExtDep externaDependency, string path, Func<TExtDep, Task> func)
        {
            await GetCircuitBreaker(externaDependency.GetType()).InvokeAsync(GetActionUrl<TExtDep>(path), async () => await func(externaDependency));
        }

        public static void ExecuteWithCircuitBreaker<TExtDep>(this TExtDep externaDependency, string path, Action<TExtDep> func)
        {
            GetCircuitBreaker(externaDependency.GetType()).Invoke(GetActionUrl<TExtDep>(path), () => func(externaDependency));
        }

        public static KeyValuePair<int, TimeSpan?>? GetCircuitState<T>(this T circuit)
        {
            var state = GetCircuitBreaker(typeof(T)) as ICircuit;
            if (state == null) return null;
            return new KeyValuePair<int, TimeSpan?>(state.Failures, state.SuspendedTime);
        }
    }
}
