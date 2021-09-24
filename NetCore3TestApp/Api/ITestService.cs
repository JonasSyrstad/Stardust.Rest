using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Stardust.Interstellar.Rest.Annotations;

namespace NetCore3TestApp.Api
{
    [Api("api")]
    public interface ITestService
    {
        [Get("test")]
        [SuccessStatusCode(HttpStatusCode.Accepted)]
        Task<string> Test([InPath] string message);

        [Get("test2")]
        Task<string> Test2([InPath] string message);

        [Get("test3")]
        [SuccessStatusCode(HttpStatusCode.Accepted)]
        string Test3([InPath] string message);

        [Get("test4")]
        string Test4([InPath] string message);

        [Post("test5")] 
        [SuccessStatusCode(HttpStatusCode.Accepted)]
        Task Test5([InQuery]string tt);
    }

    public class TestService : ITestService
    {
        public Task<string> Test(string message)
        {
            return Task.FromResult($"{DateTime.UtcNow}: {message}");
        }

        public Task<string> Test2(string message)
        {
            return Task.FromResult($"{DateTime.UtcNow}: {message}");
        }

        public string Test3(string message)
        {
            return $"{DateTime.UtcNow}: {message}";
        }

        public string Test4(string message)
        {
            return $"{DateTime.UtcNow}: {message}";
        }

        public Task Test5(string tt) 
        {
            return Task.CompletedTask;
        }
    }
}
