﻿using System;
using System.Net;
using System.Threading.Tasks;
using Stardust.Interstellar.Rest.Annotations;
using Stardust.Interstellar.Rest.Common;

namespace Stardust.Interstellar.Rest.Client.CircuitBreaker
{
    internal class NullBreaker : ICircuitBreaker, ICircuitBreakerMonitor
    {
        public ResultWrapper Execute(string path, Func<ResultWrapper> func, IServiceProvider provider)
        {
            var result = func();

            var exception = result.Error as WebException;
            if (exception != null)
            {
                try
                {
                    exception.Response?.Close();
                    exception.Response?.Dispose();
                }
                catch
                {

                }
            }
            return result;
        }

        public async Task<ResultWrapper> ExecuteAsync(string path,Func<Task<ResultWrapper>> func, IServiceProvider provider)
        {
            var result= await func();

            var exception = result.Error as WebException;
            if (exception != null)
            {
                try
                {
                    exception.Response?.Close();
                    exception.Response?.Dispose();
                }
                catch
                {
                    
                }
            }
            return result;

        }

        public T Invoke<T>(string actionUrl, Func<T> func, IServiceProvider provider)
        {
            return func();
        }

        public async Task<T> InvokeAsync<T>(string actionUrl, Func<Task<T>> func, IServiceProvider provider)
        {
            return await func();
        }

        public void Invoke(string actionUrl, Action func, IServiceProvider provider)
        {
            func();
        }

        public async Task InvokeAsync(string actionUrl, Func<Task> func, IServiceProvider provider)
        {
            await func();
        }

        public void Trip(string circuitBreakerServiceName, Exception exception, ICircuitBreakerState state, IServiceProvider provider)
        {
            provider?.GetService<ILogger>()?.Error(exception);
            var webEx = exception as WebException ?? (exception as AggregateException)?.InnerException as WebException;
            provider?.GetService<ILogger>()?.Message($"Invocation of service {circuitBreakerServiceName} failed. Action url: {webEx?.Response?.ResponseUri}");
        }

        public bool IsExceptionIgnorable(Exception exception)
        {
            return false;
        }
    }


}