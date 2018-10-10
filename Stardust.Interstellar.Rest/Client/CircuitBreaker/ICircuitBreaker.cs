using System;
using System.Threading.Tasks;

namespace Stardust.Interstellar.Rest.Client.CircuitBreaker
{
    internal interface ICircuitBreaker
    {
        ResultWrapper Execute(string actionUrl,Func<ResultWrapper> func, IServiceProvider provider);

        Task<ResultWrapper> ExecuteAsync(string actionUrl,Func<Task<ResultWrapper>> func,IServiceProvider provider);

        T Invoke<T>(string actionUrl, Func<T> func, IServiceProvider provider);

        Task<T> InvokeAsync<T>(string actionUrl, Func<Task<T>> func, IServiceProvider provider);

        void Invoke(string actionUrl, Action func, IServiceProvider provider);

        Task InvokeAsync(string actionUrl, Func<Task> func, IServiceProvider provider);
    }
}