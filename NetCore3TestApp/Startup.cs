using Microsoft.OpenApi.Models;
using NetCore3TestApp.Api;
using Stardust.Interstellar.Rest.Annotations.EndpointRouting;
using Stardust.Interstellar.Rest.Client;
using Stardust.Interstellar.Rest.Service;

namespace NetCore3TestApp
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            
            services.AddInterstellar().AddAuthentication().AddCookie().Services.AddMvc()
                .AddAsController<ITestService,TestService>()
                .UseInterstellar();
            services.AddTransient(s=>s.CreateRestClient<ITestClient>("https://localhost:44337"));
            
            // Register the endpoint test service for use with AddAsEndpoints
            services.AddScoped<IEndpointTestService, EndpointTestService>();
            
            // Register routing policies for endpoint customization
            services.AddSingleton<IEndpointRoutingPolicy>(new CachingPolicy(maxAgeSeconds: 300));
            services.AddSingleton<IEndpointRoutingPolicy>(new RateLimitingPolicy(requestsPerMinute: 60));
            services.AddSingleton<IEndpointRoutingPolicy>(new ApiCorsPolicy(allowedOrigins: "*"));
            services.AddSingleton<IEndpointRoutingPolicy>(new SecurityHeadersPolicy());
            services.AddSingleton<IEndpointRoutingPolicy>(new ApiVersionPolicy(version: "1.0"));
            
            // Register endpoints for Swagger documentation
            EndpointRoutingDocumentFilter.RegisterServiceEndpoints<IEndpointTestService>();
            
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo  { Title = "My API", Version = "v1" });
                c.IgnoreObsoleteActions();
                
                // Add the document filter for endpoint routing documentation
                c.DocumentFilter<EndpointRoutingDocumentFilter>();
            });

        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthorization();
            
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapRazorPages();
                endpoints.MapControllers();
                
                // Test the new AddAsEndpoints functionality for minimal API endpoint routing
                endpoints.AddAsEndpoints<IEndpointTestService>();
            });
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V1");
            });
        }
    }
}
