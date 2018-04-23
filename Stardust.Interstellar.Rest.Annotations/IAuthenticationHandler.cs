using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace Stardust.Interstellar.Rest.Extensions
{
    public interface IAuthenticationHandler
    {
        void Apply(HttpWebRequest req);

        Task ApplyAsync(HttpWebRequest req);
    }
}