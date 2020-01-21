using System.Net;
using System.Threading.Tasks;

namespace Stardust.Interstellar.Rest.Extensions
{
    public class NullAuthHandler : IAuthenticationHandler
    {
        public void Apply(HttpWebRequest req)
        {

        }

        public Task ApplyAsync(HttpWebRequest req)
        {
            return Task.CompletedTask;
        }

        public void BodyData(byte[] body)
        {
            
        }
    }
}