using System;
using System.Threading.Tasks;

namespace Stardust.Interstellar.Rest.Client.CircuitBreaker
{
    internal interface ICircuitBreaker
    {
        ResultWrapper Execute(string actionUrl,Func<ResultWrapper> func);

        Task<ResultWrapper> ExecuteAsync(string actionUrl,Func<Task<ResultWrapper>> func);

        T Invoke<T>(string actionUrl, Func<T> func);

        Task<T> InvokeAsync<T>(string actionUrl, Func<Task<T>> func);

        void Invoke(string actionUrl, Action func);

        Task InvokeAsync(string actionUrl, Func<Task> func);
    }
}