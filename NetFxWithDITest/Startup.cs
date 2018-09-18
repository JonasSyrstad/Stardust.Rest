using Microsoft.Extensions.DependencyInjection;
using Microsoft.Owin;
using NetFxWithDITest.Apis;
using Owin;
using Stardust.Interstellar.Rest.Client;
using Stardust.Interstellar.Rest.Dependencyinjection;
using Stardust.Interstellar.Rest.Service;

[assembly: OwinStartup(typeof(NetFxWithDITest.Startup))]

namespace NetFxWithDITest
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            app.AddDependencyInjection<Services>(ControllerTypes.Both);
            // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=316888
        }
    }

    public class Services : ServicesConfiguration
    {
        protected override IServiceCollection Configure(IServiceCollection services)
        {
            services.AddInterstellarServices();
            services.AddScoped(s => s.CreateRestClient<IDummyService>("https://localhost:44357/"));
            services.AddAsController<IDummyService, DummyService>(services.BuildServiceProvider());
            services.FinalizeRegistration();
            return services;
        }
    }
}
