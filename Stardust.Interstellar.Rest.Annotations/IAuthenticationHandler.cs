using System.Net;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace Stardust.Interstellar.Rest.Extensions
{
    public interface IAuthenticationHandler
    {
        void Apply(HttpRequestMessage req);

        Task ApplyAsync(HttpRequestMessage req);
        void BodyData(byte[] body);
    }
}