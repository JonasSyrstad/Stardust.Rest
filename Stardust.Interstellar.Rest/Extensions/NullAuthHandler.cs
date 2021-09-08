using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace Stardust.Interstellar.Rest.Extensions
{
    public class NullAuthHandler : IAuthenticationHandler
    {
        public void Apply(HttpRequestMessage req)
        {

        }

        public Task ApplyAsync(HttpRequestMessage req)
        {
            return Task.CompletedTask;
        }

        public void BodyData(byte[] body)
        {
            
        }
    }
}