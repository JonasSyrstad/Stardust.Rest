# Usage

In Startup.cs

```CS
  public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            app.AddDependencyInjection<Services>(ControllerTypes.Both);//alternatively only mvc or webapi
            // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=316888
        }
    }
```

Add a new Class 'Services' to your solution:

```CS
public class Services : ServicesConfiguration
    {
        protected override IServiceCollection Configure(IServiceCollection services)
        {
            services.AddInterstellarServices();//Add WebApi controller generation support. nuget: Install-Package Stardust.Interstellar.Rest.Service
            services.AddScoped(s => s.CreateRestClient<IDummyService>("https://localhost:44357/"));//register a rest api client
            services.AddAsController<IDummyService, DummyService>(services.BuildServiceProvider());//Create and bind WebApi controller
            services.FinalizeRegistration();//Finalize controller registration
			//Add additional service bindings
            return services;
        }
    }
```