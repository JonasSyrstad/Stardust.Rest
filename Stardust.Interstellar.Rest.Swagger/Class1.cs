using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using Stardust.Interstellar.Rest.Annotations;
using Stardust.Interstellar.Rest.Extensions;
using Stardust.Particles;
using Stardust.Particles.Collection;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Stardust.Interstellar.Rest.Swagger
{
    public static class SwaggerExtensions
    {
        public static IServiceCollection AddSwagggerDocs(this IServiceCollection services, List<SwaggerEndpoint> endpoints, Action<SwaggerGenOptions> customOptions = null)
        {
            services.AddSwaggerGen(c =>
            {
                foreach (var swaggerEndpoint in endpoints)
                {
                    var serviceDescription = swaggerEndpoint.EndpointDescription;
                    if (swaggerEndpoint.ReadDescriptionFromFile && File.Exists(swaggerEndpoint.EndpointDescription))
                        serviceDescription = File.ReadAllText(swaggerEndpoint.EndpointDescription);
                    c.SwaggerDoc(swaggerEndpoint.Version, new OpenApiInfo
                    {

                        Title = swaggerEndpoint.Title,
                        Version = swaggerEndpoint.Version,
                        Description = $"{serviceDescription}\n\n> Service build version {ConfigurationManagerHelper.GetValueOnKey("serviceVersion")} - Environment: {swaggerEndpoint.EnvironmentName}"
                    });
                }
                AddCommonSwaggerSettings(customOptions, c);
            });
            return services;
        }
        private static void AddCommonSwaggerSettings(Action<SwaggerGenOptions> customOptions, SwaggerGenOptions c)
        {
            c.AddSecurityDefinition("oauth2", new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.OAuth2,
                Flows = new OpenApiOAuthFlows
                {
                    Implicit = new OpenApiOAuthFlow
                    {
                        AuthorizationUrl =
                            new Uri(AuthorizationUrl(ConfigurationManagerHelper.GetValueOnKey("tenantId"), true)),
                        Scopes = AcceptScopeAttribute.AllScopes.Distinct().ToDictionary(
                            k => $"{ConfigurationManagerHelper.GetValueOnKey("resourceId")}/{k.Item1}", v => v.Item2)
                    }
                }
            });
            
            c.CustomSchemaIds((type) => type.ToString()
                .Replace("[", "_")
                .Replace("]", "_")
                .Replace(",", "-")
                .Replace("`", "_")
            );

            c.IgnoreObsoleteActions();
            c.IgnoreObsoleteProperties();
            c.SwaggerGeneratorOptions.OperationFilters.Add(new SecurityRequirementsOperationFilter());
            customOptions?.Invoke(c);
        }
        private static string FormatAuthorityUrl(string tenantId, bool useV2Endpoint, string type)
        {
            return $"{AadSigninUrl}/{tenantId}/oauth2/{(useV2Endpoint ? "v2.0/" : "")}{type}";
        }

        private static string AuthorizationUrl(string tenantId, bool useV2Endpoint)
        {
            return FormatAuthorityUrl(tenantId, useV2Endpoint, "authorize");
        }

        public static string AadSigninUrl { get; set; } = "https://login.microsoftonline.com";
    }
    public class SecurityRequirementsOperationFilter : IOperationFilter
    {
        public static List<string> AllScopes = new List<string>();
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {

            if (context.MethodInfo.GetCustomAttributes<AuthorizeAttribute>().ContainsElements())
            {
                var scopes = context.MethodInfo.GetCustomAttribute<AcceptScopeAttribute>(true)
                    ?.Scopes.ToList() ?? new List<string>();
                var parentScopes = context.MethodInfo.DeclaringType?.GetCustomAttribute<DefaultAcceptScopeAttribute>(true)?.Scopes;
                if (parentScopes != null)
                    scopes.AddRange(parentScopes);

                var scopeList = GetScopes(scopes);
                AllScopes.AddRange(scopeList);

            }
            operation.OperationId = $"{context.MethodInfo.DeclaringType?.Name}.{context.MethodInfo.Name}.{context.ApiDescription.HttpMethod}.{context.ApiDescription.ParameterDescriptions?.Count ?? 0}";
            AddMetadata(operation, context);
            if (operation.Parameters == null) operation.Parameters = new List<OpenApiParameter>();
            //operation.Parameters.Add(new NonBodyParameter { Name = "x-supportCode", In = "header", Type = "string", Description = "A correlation token to use when looking in the logs.", Required = false });
            operation.Parameters.Add(new OpenApiParameter() { Name = "request-id", In = ParameterLocation.Header, Description = "A correlation token to use when looking in the logs.", Required = false, Schema = context.SchemaGenerator.GenerateSchema(typeof(string), context.SchemaRepository) });
        }

        internal static string[] GetScopes(List<string> scopes)
        {
            if (scopes.ContainsElements())
            {
                var sc = scopes.Distinct().Select(s => $"{ConfigurationManagerHelper.GetValueOnKey("resourceId")}/{s}").ToArray();
                return sc;
            }
            if (ConfigurationManagerHelper.GetValueOnKey("scopes").ContainsCharacters())
                return ConfigurationManagerHelper.GetValueOnKey("scopes").Split(' ');
            return new[] { $"{ConfigurationManagerHelper.GetValueOnKey("resourceId")}/user_impersonation" };
        }

        private static void AddMetadata(OpenApiOperation operation, OperationFilterContext context)
        {
            var endpointDescription = context.MethodInfo
                .GetCustomAttribute<ServiceDescriptionAttribute>();
            var serviceDescription = context.MethodInfo.DeclaringType?
                .GetCustomAttribute<ServiceDescriptionAttribute>();
            
           
            operation.Description =$"{endpointDescription?.Description}";
            operation.Summary =
                $"{endpointDescription?.Summary}{(endpointDescription?.Summary == null ? "" : $"{Environment.NewLine}{Environment.NewLine}")}{serviceDescription?.Summary}";
            operation.Deprecated = endpointDescription?.IsDeprecated ?? false;
            var tags = new List<string>();
            if (endpointDescription?.Tags != null) tags.AddRange(endpointDescription.Tags.Split(';'));
            if (serviceDescription?.Tags != null) tags.AddRange(serviceDescription.Tags.Split(';'));
            if (tags.ContainsElements())
                operation.Tags = tags.Select(t => new OpenApiTag { Name = t }).ToArray();

        }

        
    }
    /// <summary>
    /// Set additional scope validation. This comes in addition to the one defined on the interface level.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class AcceptScopeAttribute : Stardust.Interstellar.Rest.Annotations.HeaderInspectorAttributeBase, Stardust.Interstellar.Rest.Annotations.Service.ICreateImplementationAttribute
    {
        public bool AnyScope { get; set; }
        internal static List<Tuple<string, string>> _allScopes = new List<Tuple<string, string>>();
        public static Tuple<string, string>[] AllScopes => _allScopes.Distinct().ToArray();
        public string[] Scopes { get; set; }

        public AcceptScopeAttribute(params string[] scopes)
        {
            Scopes = scopes;
            _allScopes.AddRange(scopes.Select(s => (s, s).ToTuple()));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="scopes">List of scopes to accept, first item is the scope name, second is the display name</param>
        public AcceptScopeAttribute(params Tuple<string, string>[] scopes)
        {
            Scopes = scopes.Select(s => s.Item1).ToArray();
            _allScopes.AddRange(scopes);
        }

        public override IHeaderHandler[] GetHandlers(IServiceProvider serviceLocator)
        {
            var handler = serviceLocator.GetService<AcceptScopeHandler>();
            handler.SetAcceptedScopes(Scopes, AnyScope);
            return new IHeaderHandler[] { handler };
        }

        public CustomAttributeBuilder CreateAttribute()
        {
            var ctor = this.GetType().GetConstructor(new[] { typeof(string[]) });
            var prop = GetType().GetProperty("AnyScope");
            return new CustomAttributeBuilder(ctor, new object[] { Scopes }, new[] { prop }, new object[] { AnyScope });
        }
    }

    /// <summary>
    /// Defines the minimum scope requirements for the service
    /// </summary>
    [AttributeUsage(AttributeTargets.Interface)]
    public class DefaultAcceptScopeAttribute : Stardust.Interstellar.Rest.Annotations.HeaderInspectorAttributeBase, Stardust.Interstellar.Rest.Annotations.Service.ICreateImplementationAttribute
    {
        public bool AllScope { get; set; }


        public string[] Scopes { get; set; }

        public DefaultAcceptScopeAttribute(params string[] scopes)
        {
            Scopes = scopes;
            try
            {
                AcceptScopeAttribute._allScopes.AddRange(scopes.Select(s => (s, s).ToTuple()));
            }
            catch (Exception)
            {

            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="scopes">List of scopes to accept, first item is the scope name, second is the display name</param>
        public DefaultAcceptScopeAttribute(params Tuple<string, string>[] scopes)
        {
            Scopes = scopes.Select(s => s.Item1).ToArray();
            AcceptScopeAttribute._allScopes.AddRange(scopes);
        }

        public override IHeaderHandler[] GetHandlers(IServiceProvider serviceLocator)
        {
            var handler = serviceLocator.GetService<AcceptScopeHandler>();
            handler.SetAcceptedScopes(Scopes, !AllScope);
            return new IHeaderHandler[] { handler };
        }

        public CustomAttributeBuilder CreateAttribute()
        {
            var ctor = GetType().GetConstructor(new[] { typeof(string[]) });
            var prop = GetType().GetProperty("AllScope");
            return new CustomAttributeBuilder(ctor, new object[] { Scopes }, new[] { prop }, new object[] { AllScope });
        }
    }
}
