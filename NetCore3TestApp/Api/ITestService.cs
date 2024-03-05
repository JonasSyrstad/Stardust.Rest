using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Stardust.Interstellar.Rest.Annotations;

namespace NetCore3TestApp.Api
{

    [Api("api/{id}")]
    public interface ITestClient
    {
        [Get("test")]
        [SuccessStatusCode(HttpStatusCode.Accepted)]
        Task<string> Test([InPath] string id, [InPath] string message);

        [Get("paramsTest")]
        Task<string> Params([InPath] string id, [InQuery()] Dictionary<string, string> prefixedParameters);
    }

    [Api("api/{id}")]
    public interface ITestService
    {
        [Get("test")]
        [SuccessStatusCode(HttpStatusCode.Accepted)]
        Task<string> Test([InPath] string id, [InPath] string message);

        [Get("test2")]
        Task<string> Test2([InPath] string id, [InPath] string message);

        [Get("test3")]
        [SuccessStatusCode(HttpStatusCode.Accepted)]
        string Test3([InPath] string id, [InPath] string message);

        [Get("test4")]
        string Test4([InPath] string id, [InPath] string message);

        [Post("test5")] 
        [SuccessStatusCode(HttpStatusCode.Accepted)]
        Task Test5([InPath] string id, [InQuery]string tt);

        [Get("paramsTest")]
        Task<string> Params([InPath] string id, [InQuery()] Dictionary<string, string> prefixedParameters);
    }

    public class TestService : ITestService
    {
        public Task<string> Test(string id, string message)
        {
            return Task.FromResult($"{DateTime.UtcNow}: '{message}' with id '{id}'");
        }

        public Task<string> Test2(string id, string message)
        {
            return Task.FromResult($"{DateTime.UtcNow}: {message}");
        }

        public string Test3(string id, string message)
        {
            return $"{DateTime.UtcNow}: {message}";
        }

        public string Test4(string id, string message)
        {
            return $"{DateTime.UtcNow}: {message}";
        }

        public Task Test5(string id, string tt) 
        {
            return Task.CompletedTask;
        }

        public async Task<string> Params(string id, Dictionary<string, string> prefixedParameters)
        {
            return string.Join("&", prefixedParameters.Select(p => $"{p.Key}={p.Value}"))+" "+ id;
        }
    }
}
