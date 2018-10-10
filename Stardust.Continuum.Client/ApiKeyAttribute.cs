using Stardust.Interstellar.Rest.Annotations;
using Stardust.Interstellar.Rest.Extensions;
using System;
using System.Net;
using System.Threading.Tasks;

namespace Stardust.Continuum.Client
{
    public class ApiKeyAttribute : AuthenticationInspectorAttributeBase, IAuthenticationHandler
    {
        public override IAuthenticationHandler GetHandler(IServiceProvider provider)
        {
            return this;
        }

        public void Apply(HttpWebRequest req)
        {
            req.Headers.Add("Authorization", "ApiKey " + LogStreamConfig.ApiKey);
        }

        public Task ApplyAsync(HttpWebRequest req)
        {
            req.Headers.Add("Authorization", "ApiKey " + LogStreamConfig.ApiKey);
            return Task.CompletedTask;
        }
    }
}
