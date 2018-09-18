using Microsoft.Extensions.DependencyInjection;
using Stardust.Interstellar.Rest.Common;

namespace Stardust.Interstellar.Rest.Dependencyinjection
{
    public abstract class ServicesConfiguration
    {
        internal IServiceCollection ConfigureInterstellar(IServiceCollection services)
        {
            services.AddInterstellarClient();
            return Configure(services);
        }

        protected abstract IServiceCollection Configure(IServiceCollection services);
    }
}