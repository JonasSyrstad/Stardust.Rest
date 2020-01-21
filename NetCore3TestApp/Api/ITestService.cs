using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Stardust.Interstellar.Rest.Annotations;

namespace NetCore3TestApp.Api
{
    [Api("api")]
    public interface ITestService
    {
        [Get("test")]
        Task<string> Test([InPath] string message);
    }

    public class TestService : ITestService
    {
        public Task<string> Test(string message)
        {
            return Task.FromResult($"{DateTime.UtcNow}: {message}");
        }
    }
}
