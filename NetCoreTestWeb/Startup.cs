﻿using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NetCoreTestWeb.Controllers;
using Stardust.Aadb2c.AuthenticationFilter;
using Stardust.Aadb2c.AuthenticationFilter.Core;
using Stardust.Interstellar.Rest.Annotations;
using Stardust.Interstellar.Rest.Client;
using Stardust.Interstellar.Rest.Common;
using Stardust.Interstellar.Rest.Extensions;
using Stardust.Interstellar.Rest.Service;
using Stardust.Particles;
using Swashbuckle.AspNetCore.Swagger;
using System;
using System.Collections.Specialized;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using ILogger = Stardust.Interstellar.Rest.Common.ILogger;

namespace NetCoreTestWeb
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;

        }

        public IConfiguration Configuration { get; }
        public static IWebHostBuilder Host { get; set; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            ServiceFactory.ThrowOnException = true;
            services.AddInterstellar().AddHttpContextAccessor().AddLogging()
                .AddSingleton(s => new Locator(s))
                .AddSingleton<ILogger, LogWrapper>();
            services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddB2CAuthentication("OAuth2", "Azure B2C authentication");
            services.AddScoped(s => s.CreateRestClient<IDummyClient>("https://localhost:44305/jonas.syrstad%40dnvgl.com/ledger"));

            var builder = services.AddMvc().AddAsController<IMyServies, MyServies>().UseInterstellar();

            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new Info { Title = "My API", Version = "v1" });
                c.IgnoreObsoleteActions();
            });


        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();

            }
            app.AddConfigurationManager(new ConfigManager());

            app.UseMvc();
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V1");
            });

        }
    }

    [Api("test")]
    [DummyAuth]
    public interface IDummyClient
    {
        [Get("action","test")]
        Task<string> Echo([InQuery] string message);
    }

    public class DummyAuthAttribute : Attribute, IAuthenticationInspector, IAuthenticationHandler
    {
        private readonly IServiceProvider _provider;

        public DummyAuthAttribute(IServiceProvider provider)
        {
            _provider = provider;
        }

        public DummyAuthAttribute()
        {
        }

        public IAuthenticationHandler GetHandler(IServiceProvider provider)
        {
            return new DummyAuthAttribute(provider);
        }

        public void Apply(HttpRequestMessage req)
        {

        }

        public Task ApplyAsync(HttpRequestMessage req)
        {
            return Task.CompletedTask; ;
        }

        public void BodyData(byte[] body)
        {
            
        }
    }

    public class ConfigManager : IConfigurationReader
    {
        public NameValueCollection AppSettings
        {
            get
            {
                return new NameValueCollection
                {
                    { "aadTenant","dnvglb2ctest.onmicrosoft.com"},
                    { "tenantId","ed815121-cdfa-4097-b524-e2b23cd36eb6"},
                    { "aadPolicy","B2C_1A_SignInWithADFSIdp"},
                    { "audience","a4a8e726-c1cc-407c-83a0-4ce37f1ce130"},
                    { "audience_v1","https://dnvglb2ctest.onmicrosoft.com/efb3e529-2f80-458b-aedf-7f4c8c794b45"},
                    { "swaggerClientId","dfd50e76-5d28-4c57-bcc2-f5e46d48568c"},
                    { "resourceId","https://dnvglb2ctest.onmicrosoft.com/a4a8e726-c1cc-407c-83a0-4ce37f1ce130"},
                    { "issuerHostName","https://login.microsoftonline.com/ed815121-cdfa-4097-b524-e2b23cd36eb6/v2.0"},
                    { "issuerHostName_v1","https://sts.windows.net/ed815121-cdfa-4097-b524-e2b23cd36eb6/"},
                    { "idpMetadata","https://login.microsoftonline.com/ed815121-cdfa-4097-b524-e2b23cd36eb6/federationmetadata/2007-06/federationmetadata.xml"}
                };
            }
        }
    }


    public class MyFilter : IAuthorizationFilter
    {

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var v = context.HttpContext.Request.Headers["Authorization"].FirstOrDefault();
            if (v != null)
            {
                var user = TokenValidator.Validate(v.Split(' ')[1]);
                context.HttpContext.User = user;
            }
        }
    }


    public class LogWrapper : ILogger
    {
        private readonly Microsoft.Extensions.Logging.ILogger _logger;

        //public LogWrapper(Microsoft.Extensions.Logging.ILogger logger)
        //{
        //    _logger = logger;
        //}
        public void Error(Exception error)
        {
            _logger?.LogError(error, error.Message);
        }

        public void Message(string message)
        {
            _logger?.LogDebug(message);
        }

        public void Message(string format, params object[] args)
        {
            Message(string.Format(format, args));
        }
    }
}
