using System.Net;
using Stardust.Interstellar.Rest.Annotations;
using Stardust.Interstellar.Rest.Extensions;

namespace Stardust.Continuum.Client
{
    public class ApiKeyAttribute : AuthenticationInspectorAttributeBase, IAuthenticationHandler
    {
        public override IAuthenticationHandler GetHandler()
        {
            return this;
        }

        public void Apply(HttpWebRequest req)
        {
            req.Headers.Add("Authorization", "ApiKey " + LogStreamConfig.ApiKey);
        }
    }
}
