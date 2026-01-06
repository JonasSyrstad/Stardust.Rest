using System.Net;
using System.Reflection;
using Microsoft.OpenApi.Models;
using Stardust.Interstellar.Rest.Annotations;
using Stardust.Interstellar.Rest.Annotations.EndpointRouting;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Stardust.Interstellar.Rest.Service
{
    /// <summary>
    /// Swagger document filter that documents endpoints created by AddAsEndpoints.
    /// </summary>
    public class EndpointRoutingDocumentFilter : IDocumentFilter
    {
        private static readonly List<EndpointDocumentation> _registeredEndpoints = [];
        private readonly IServiceProvider _serviceProvider;

        public EndpointRoutingDocumentFilter(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        /// <summary>
        /// Registers endpoint documentation for a service interface.
        /// Call this during startup to register endpoints for Swagger documentation.
        /// </summary>
        public static void RegisterServiceEndpoints<TService>() where TService : class
        {
            var interfaceType = typeof(TService);
            var routePrefix = GetRoutePrefix(interfaceType);
            var methods = interfaceType.GetMethods().Length == 0
                ? interfaceType.GetInterfaces().FirstOrDefault()?.GetMethods() ?? []
                : interfaceType.GetMethods();

            foreach (var method in methods)
            {
                var doc = CreateEndpointDocumentation(method, routePrefix);
                if (doc != null)
                {
                    _registeredEndpoints.Add(doc);
                }
            }
        }

        private static EndpointDocumentation? CreateEndpointDocumentation(MethodInfo method, IRoutePrefix? routePrefix)
        {
            var verbAttribute = method.GetCustomAttribute<VerbAttribute>();
            var httpMethod = GetHttpMethod(method, verbAttribute);
            var routeTemplate = BuildRouteTemplate(method, routePrefix, verbAttribute);
            var serviceDescription = method.GetCustomAttribute<ServiceDescriptionAttribute>();
            var interfaceDescription = method.DeclaringType?.GetCustomAttribute<ServiceDescriptionAttribute>();

            var parameters = new List<EndpointParameterDoc>();
            foreach (var param in method.GetParameters())
            {
                var inAttribute = param.GetCustomAttribute<InAttribute>();
                var inclusionType = inAttribute?.InclutionType ?? DetermineDefaultInclusionType(param, method);
                
                // Use custom name if specified, otherwise use parameter name
                var effectiveName = !string.IsNullOrWhiteSpace(inAttribute?.Name) 
                    ? inAttribute.Name 
                    : param.Name ?? string.Empty;
                
                // Get description from InAttribute
                var description = inAttribute?.Description;
                
                parameters.Add(new EndpointParameterDoc
                {
                    Name = effectiveName,
                    ParameterName = param.Name ?? string.Empty,
                    Description = description,
                    Type = param.ParameterType,
                    Source = inclusionType,
                    IsRequired = !param.ParameterType.IsValueType || Nullable.GetUnderlyingType(param.ParameterType) == null,
                    IsNullable = !param.ParameterType.IsValueType || Nullable.GetUnderlyingType(param.ParameterType) != null
                });
            }

            var returnType = method.ReturnType;
            if (typeof(Task).IsAssignableFrom(returnType) && returnType.IsGenericType)
            {
                returnType = returnType.GetGenericArguments().FirstOrDefault() ?? typeof(void);
            }

            var statusCodeAttribute = method.GetCustomAttribute<SuccessStatusCodeAttribute>();

            // Collect policy names from method and interface level
            var policyNames = new List<string>();
            var methodPolicies = method.GetCustomAttributes<EndpointRoutingPolicyAttribute>();
            foreach (var attr in methodPolicies)
            {
                if (!string.IsNullOrWhiteSpace(attr.PolicyName))
                    policyNames.Add(attr.PolicyName);
            }
            var interfacePolicies = method.DeclaringType?.GetCustomAttributes<EndpointRoutingPolicyAttribute>();
            if (interfacePolicies != null)
            {
                foreach (var attr in interfacePolicies)
                {
                    if (!string.IsNullOrWhiteSpace(attr.PolicyName))
                        policyNames.Add(attr.PolicyName);
                }
            }

            return new EndpointDocumentation
            {
                Route = "/" + routeTemplate.TrimStart('/'),
                HttpMethod = httpMethod.ToLowerInvariant(),
                OperationId = method.Name.EndsWith("Async") ? method.Name[..^5] : method.Name,
                Summary = serviceDescription?.Summary ?? serviceDescription?.Description,
                Description = serviceDescription?.Description,
                Tags = GetTags(interfaceDescription, serviceDescription, method.DeclaringType),
                Parameters = parameters,
                ReturnType = returnType,
                SuccessStatusCode = statusCodeAttribute?.StatusCode ?? HttpStatusCode.OK,
                IsDeprecated = serviceDescription?.IsDeprecated ?? method.GetCustomAttribute<ObsoleteAttribute>() != null,
                PolicyNames = policyNames.Distinct().ToList()
            };
        }

        private static string[] GetTags(ServiceDescriptionAttribute? interfaceDesc, ServiceDescriptionAttribute? methodDesc, Type? declaringType)
        {
            var tags = new List<string>();
            
            if (!string.IsNullOrWhiteSpace(interfaceDesc?.Tags))
            {
                tags.AddRange(interfaceDesc.Tags.Split(',').Select(t => t.Trim()));
            }

            if (!string.IsNullOrWhiteSpace(methodDesc?.Tags))
            {
                tags.AddRange(methodDesc.Tags.Split(',').Select(t => t.Trim()));
            }

            if (tags.Count == 0)
            {
                tags.Add(declaringType?.Name.TrimStart('I') ?? "Api");
            }

            return tags.Distinct().ToArray();
        }

        public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
        {
            // Get all registered policies from DI
            var policies = _serviceProvider.GetServices<IEndpointRoutingPolicy>().ToList();

            foreach (var endpoint in _registeredEndpoints)
            {
                var path = endpoint.Route;
                
                if (!swaggerDoc.Paths.ContainsKey(path))
                {
                    swaggerDoc.Paths.Add(path, new OpenApiPathItem());
                }

                var pathItem = swaggerDoc.Paths[path];
                var operation = CreateOperation(endpoint, context, policies);

                var operationType = endpoint.HttpMethod.ToUpperInvariant() switch
                {
                    "GET" => OperationType.Get,
                    "POST" => OperationType.Post,
                    "PUT" => OperationType.Put,
                    "DELETE" => OperationType.Delete,
                    "PATCH" => OperationType.Patch,
                    "HEAD" => OperationType.Head,
                    "OPTIONS" => OperationType.Options,
                    _ => OperationType.Get
                };

                pathItem.Operations[operationType] = operation;
            }
        }

        private static OpenApiOperation CreateOperation(EndpointDocumentation endpoint, DocumentFilterContext context, List<IEndpointRoutingPolicy> policies)
        {
            // Collect OpenAPI settings from matching policies
            var policySettings = new List<OpenApiPolicySettings>();
            foreach (var policyName in endpoint.PolicyNames)
            {
                var matchingPolicy = policies.FirstOrDefault(p =>
                    string.Equals(p.PolicyName, policyName, StringComparison.OrdinalIgnoreCase));
                
                var settings = matchingPolicy?.GetOpenApiSettings();
                if (settings != null)
                {
                    policySettings.Add(settings);
                }
            }

            // Merge tags from endpoint and policies
            var allTags = new List<string>(endpoint.Tags);
            foreach (var settings in policySettings)
            {
                if (settings.Tags != null && settings.Tags.Length > 0)
                {
                    allTags.AddRange(settings.Tags);
                }
            }

            // Check for deprecation from policies
            var isDeprecated = endpoint.IsDeprecated;
            string? deprecationMessage = null;
            foreach (var settings in policySettings)
            {
                if (settings.IsDeprecated)
                {
                    isDeprecated = true;
                    deprecationMessage = settings.DeprecationMessage;
                    break;
                }
            }

            // Merge descriptions from policies (append to existing)
            var description = endpoint.Description;
            foreach (var settings in policySettings)
            {
                if (!string.IsNullOrWhiteSpace(settings.Description))
                {
                    if (string.IsNullOrEmpty(description))
                    {
                        description = settings.Description;
                    }
                    else
                    {
                        description = $"{description}\n\n{settings.Description}";
                    }
                }
            }

            // Use policy summary if endpoint doesn't have one
            var summary = endpoint.Summary;
            foreach (var settings in policySettings)
            {
                if (!string.IsNullOrWhiteSpace(settings.Summary) && string.IsNullOrWhiteSpace(summary))
                {
                    summary = settings.Summary;
                    break;
                }
            }

            var operation = new OpenApiOperation
            {
                OperationId = endpoint.OperationId,
                Summary = summary,
                Description = description,
                Deprecated = isDeprecated,
                Tags = allTags.Distinct().Select(t => new OpenApiTag { Name = t }).ToList(),
                Parameters = [],
                Responses = new OpenApiResponses()
            };

            // Add parameters
            OpenApiRequestBody? requestBody = null;
            foreach (var param in endpoint.Parameters)
            {
                if (param.Source == InclutionTypes.Body)
                {
                    var schema = context.SchemaGenerator.GenerateSchema(param.Type, context.SchemaRepository);
                    requestBody = new OpenApiRequestBody
                    {
                        Required = param.IsRequired,
                        Description = param.Description,
                        Content = new Dictionary<string, OpenApiMediaType>
                        {
                            ["application/json"] = new OpenApiMediaType { Schema = schema }
                        }
                    };
                }
                else
                {
                    var paramLocation = param.Source switch
                    {
                        InclutionTypes.Path => ParameterLocation.Path,
                        InclutionTypes.Query => ParameterLocation.Query,
                        InclutionTypes.Header => ParameterLocation.Header,
                        _ => ParameterLocation.Query
                    };

                    operation.Parameters.Add(new OpenApiParameter
                    {
                        Name = param.Name,
                        Description = param.Description,
                        In = paramLocation,
                        Required = param.Source == InclutionTypes.Path || param.IsRequired,
                        Schema = context.SchemaGenerator.GenerateSchema(param.Type, context.SchemaRepository)
                    });
                }
            }

            if (requestBody != null)
            {
                operation.RequestBody = requestBody;
            }

            // Add responses
            var successCode = ((int)endpoint.SuccessStatusCode).ToString();
            if (endpoint.ReturnType != typeof(void))
            {
                var responseSchema = context.SchemaGenerator.GenerateSchema(endpoint.ReturnType, context.SchemaRepository);
                operation.Responses[successCode] = new OpenApiResponse
                {
                    Description = "Success",
                    Content = new Dictionary<string, OpenApiMediaType>
                    {
                        ["application/json"] = new OpenApiMediaType { Schema = responseSchema }
                    }
                };
            }
            else
            {
                operation.Responses[successCode] = new OpenApiResponse { Description = "Success" };
            }

            // Add error responses
            operation.Responses["401"] = new OpenApiResponse { Description = "Unauthorized" };
            operation.Responses["404"] = new OpenApiResponse { Description = "Not Found" };
            operation.Responses["500"] = new OpenApiResponse { Description = "Internal Server Error" };

            // Add additional responses from policies
            foreach (var settings in policySettings)
            {
                if (settings.AdditionalResponses != null)
                {
                    foreach (var response in settings.AdditionalResponses)
                    {
                        var statusCodeStr = response.Key.ToString();
                        if (!operation.Responses.ContainsKey(statusCodeStr))
                        {
                            if (response.Value.ResponseType != null)
                            {
                                var responseSchema = context.SchemaGenerator.GenerateSchema(response.Value.ResponseType, context.SchemaRepository);
                                operation.Responses[statusCodeStr] = new OpenApiResponse
                                {
                                    Description = response.Value.Description ?? $"Status {response.Key}",
                                    Content = new Dictionary<string, OpenApiMediaType>
                                    {
                                        [response.Value.ContentType ?? "application/json"] = new OpenApiMediaType { Schema = responseSchema }
                                    }
                                };
                            }
                            else
                            {
                                operation.Responses[statusCodeStr] = new OpenApiResponse
                                {
                                    Description = response.Value.Description ?? $"Status {response.Key}"
                                };
                            }
                        }
                    }
                }
            }

            return operation;
        }

        private static IRoutePrefix? GetRoutePrefix(Type interfaceType)
        {
#pragma warning disable CS0618
            return interfaceType.GetCustomAttribute<ApiAttribute>()
                ?? interfaceType.GetInterfaces().FirstOrDefault()?.GetCustomAttribute<ApiAttribute>()
                ?? (IRoutePrefix?)interfaceType.GetCustomAttribute<IRoutePrefixAttribute>()
                ?? interfaceType.GetInterfaces().FirstOrDefault()?.GetCustomAttribute<IRoutePrefixAttribute>();
#pragma warning restore CS0618
        }

        private static string GetHttpMethod(MethodInfo method, VerbAttribute? verbAttribute)
        {
            if (verbAttribute != null) return verbAttribute.Verb ?? "GET";
            var attributes = method.GetCustomAttributes(true);
            if (attributes.OfType<GetAttribute>().Any()) return "GET";
            if (attributes.OfType<PostAttribute>().Any()) return "POST";
            if (attributes.OfType<PutAttribute>().Any()) return "PUT";
            if (attributes.OfType<DeleteAttribute>().Any()) return "DELETE";
            if (attributes.OfType<PatchAttribute>().Any()) return "PATCH";
            return "GET";
        }

        private static string BuildRouteTemplate(MethodInfo method, IRoutePrefix? routePrefix, VerbAttribute? verbAttribute)
        {
#pragma warning disable CS0618
            var methodRoute = verbAttribute?.Route ?? method.GetCustomAttribute<IRouteAttribute>()?.Template ?? "";
#pragma warning restore CS0618
            var prefix = routePrefix?.Prefix ?? "";
#pragma warning disable CS0612
            if (routePrefix?.IncludeTypeName == true)
#pragma warning restore CS0612
            {
                var interfaceType = method.DeclaringType;
                var typeName = interfaceType?.GetGenericArguments().FirstOrDefault()?.Name.ToLower()
                    ?? interfaceType?.GetInterfaces().FirstOrDefault()?.GetGenericArguments().FirstOrDefault()?.Name.ToLower()
                    ?? interfaceType?.Name.TrimStart('I').ToLower() ?? "api";
                prefix = string.IsNullOrEmpty(prefix) ? typeName : $"{prefix}/{typeName}";
            }

            if (string.IsNullOrEmpty(prefix)) return string.IsNullOrEmpty(methodRoute) ? method.Name : methodRoute;
            if (string.IsNullOrEmpty(methodRoute)) return prefix;
            return $"{prefix}/{methodRoute}".Replace("//", "/");
        }

        private static InclutionTypes DetermineDefaultInclusionType(ParameterInfo param, MethodInfo method)
        {
            var template = method.GetCustomAttribute<VerbAttribute>()?.Route ?? "";
            if (template.Contains("{" + param.Name + "}") || template.Contains("{" + param.Name + ":"))
                return InclutionTypes.Path;
            if (param.ParameterType.IsClass && param.ParameterType != typeof(string))
                return InclutionTypes.Body;
            return InclutionTypes.Query;
        }
    }

    internal class EndpointDocumentation
    {
        public string Route { get; set; } = string.Empty;
        public string HttpMethod { get; set; } = "GET";
        public string OperationId { get; set; } = string.Empty;
        public string? Summary { get; set; }
        public string? Description { get; set; }
        public string[] Tags { get; set; } = [];
        public List<EndpointParameterDoc> Parameters { get; set; } = [];
        public Type ReturnType { get; set; } = typeof(void);
        public HttpStatusCode SuccessStatusCode { get; set; } = HttpStatusCode.OK;
        public bool IsDeprecated { get; set; }
        public List<string> PolicyNames { get; set; } = [];
    }

    internal class EndpointParameterDoc
    {
        /// <summary>
        /// Gets or sets the effective name to use for the parameter (custom name if specified, otherwise parameter name).
        /// </summary>
        public string Name { get; set; } = string.Empty;
        
        /// <summary>
        /// Gets or sets the C# parameter name.
        /// </summary>
        public string ParameterName { get; set; } = string.Empty;
        
        /// <summary>
        /// Gets or sets the description for the parameter to be used in generated documentation.
        /// </summary>
        public string? Description { get; set; }
        
        public Type Type { get; set; } = typeof(object);
        public InclutionTypes Source { get; set; }
        public bool IsRequired { get; set; }
        public bool IsNullable { get; set; }
    }
}
