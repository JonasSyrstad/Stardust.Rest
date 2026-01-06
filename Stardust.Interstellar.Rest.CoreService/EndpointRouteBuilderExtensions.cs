using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Stardust.Interstellar.Rest.Annotations;
using Stardust.Interstellar.Rest.Annotations.EndpointRouting;
using Stardust.Interstellar.Rest.Annotations.Service;
using Stardust.Interstellar.Rest.Common;
using Stardust.Interstellar.Rest.Extensions;

#if NET6_0_OR_GREATER

namespace Stardust.Interstellar.Rest.Service
{
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Http.Metadata;
    using Microsoft.AspNetCore.Routing;

    /// <summary>
    /// Extension methods for adding service interfaces as minimal API endpoints.
    /// </summary>
    public static class EndpointRouteBuilderExtensions
    {
        private static readonly ConcurrentDictionary<Type, ConcurrentDictionary<string, EndpointActionWrapper>> _actionCache 
            = new ConcurrentDictionary<Type, ConcurrentDictionary<string, EndpointActionWrapper>>();

        /// <summary>
        /// Maps the service interface methods as minimal API endpoints using the attributes defined on the interface.
        /// </summary>
        /// <typeparam name="TService">The service interface type decorated with routing attributes.</typeparam>
        /// <typeparam name="TImplementation">The implementation type for the service interface.</typeparam>
        /// <param name="endpoints">The endpoint route builder.</param>
        /// <returns>The endpoint route builder for chaining.</returns>
        public static IEndpointRouteBuilder AddAsEndpoints<TService, TImplementation>(this IEndpointRouteBuilder endpoints)
            where TService : class
            where TImplementation : class, TService
        {
            endpoints.ServiceProvider.GetRequiredService<IServiceCollection>()?.AddScoped<TService, TImplementation>();
            return AddAsEndpointsInternal<TService>(endpoints);
        }

        /// <summary>
        /// Maps the service interface methods as minimal API endpoints using the attributes defined on the interface.
        /// Requires the service to be registered in DI separately.
        /// </summary>
        /// <typeparam name="TService">The service interface type decorated with routing attributes.</typeparam>
        /// <param name="endpoints">The endpoint route builder.</param>
        /// <returns>The endpoint route builder for chaining.</returns>
        public static IEndpointRouteBuilder AddAsEndpoints<TService>(this IEndpointRouteBuilder endpoints)
            where TService : class
        {
            return AddAsEndpointsInternal<TService>(endpoints);
        }

        private static IEndpointRouteBuilder AddAsEndpointsInternal<TService>(IEndpointRouteBuilder endpoints)
            where TService : class
        {
            var interfaceType = typeof(TService);
            InitializeServiceActions(interfaceType);
            
            var routePrefix = GetRoutePrefix(interfaceType);
            var methods = interfaceType.GetMethods().Length == 0
                ? interfaceType.GetInterfaces().FirstOrDefault()?.GetMethods() ?? Array.Empty<MethodInfo>()
                : interfaceType.GetMethods();

            foreach (var method in methods)
            {
                MapMethodToEndpoint<TService>(endpoints, method, routePrefix);
            }

            return endpoints;
        }

        private static void InitializeServiceActions(Type interfaceType)
        {
            if (_actionCache.ContainsKey(interfaceType)) return;

            var newWrapper = new ConcurrentDictionary<string, EndpointActionWrapper>();
            var assemblyThrottler = interfaceType.Assembly.GetCustomAttributes<ThrottlingAttribute>().FirstOrDefault();
            var serviceThrottler = interfaceType.GetCustomAttributes<ThrottlingAttribute>().FirstOrDefault();
            var classInitializers = interfaceType.GetCustomAttributes<ServiceInitializerAttribute>().ToArray();
            var errorHandlerAttribute = interfaceType.GetCustomAttribute<ErrorHandlerAttribute>();

            var methods = interfaceType.GetMethods().Length == 0
                ? interfaceType.GetInterfaces().FirstOrDefault()?.GetMethods() ?? Array.Empty<MethodInfo>()
                : interfaceType.GetMethods();

            foreach (var methodInfo in methods)
            {
                var methodThrottler = methodInfo.GetCustomAttributes<ThrottlingAttribute>().FirstOrDefault();
                var methodInitializers = methodInfo.GetCustomAttributes<ServiceInitializerAttribute>().ToList();
                methodInitializers.AddRange(classInitializers);

                var actionName = GetActionName(methodInfo.Name);
                var action = new EndpointActionWrapper
                {
                    Name = actionName,
                    Method = methodInfo,
                    ReturnType = methodInfo.ReturnType,
                    Parameters = BuildParameterInfo(methodInfo),
                    DefaultResponseCode = methodInfo.GetCustomAttribute<SuccessStatusCodeAttribute>()?.StatusCode,
                    Interceptors = methodInfo.GetCustomAttributes().OfType<InputInterceptorAttribute>().ToArray(),
                    Initializers = methodInitializers,
                    CustomHandlers = GetHeaderInspectors(methodInfo),
                    ErrorHandlerAttribute = errorHandlerAttribute
                };

                // Set throttler based on hierarchy
                if (methodThrottler != null)
                    action.Throttler = methodThrottler.GetManager(AppliesToTypes.Method);
                else if (serviceThrottler != null)
                    action.Throttler = serviceThrottler.GetManager(AppliesToTypes.Service);
                else if (assemblyThrottler != null)
                    action.Throttler = assemblyThrottler.GetManager(AppliesToTypes.Host);

                newWrapper.TryAdd(actionName, action);
            }

            _actionCache.TryAdd(interfaceType, newWrapper);
        }

        private static List<IHeaderInspector> GetHeaderInspectors(MethodInfo methodInfo)
        {
            return methodInfo.GetCustomAttributes().OfType<IHeaderInspector>().ToList();
        }

        private static List<EndpointParameterWrapper> BuildParameterInfo(MethodInfo methodInfo)
        {
            var parameters = new List<EndpointParameterWrapper>();
            var template = methodInfo.GetCustomAttribute<VerbAttribute>()?.Route ?? "";

            foreach (var parameterInfo in methodInfo.GetParameters())
            {
                var inAttribute = parameterInfo.GetCustomAttribute<InAttribute>(true);
                var inclusionType = inAttribute?.InclutionType ?? DetermineDefaultInclusionType(parameterInfo, template);

                parameters.Add(new EndpointParameterWrapper
                {
                    Name = parameterInfo.Name,
                    CustomName = inAttribute?.Name,
                    Description = inAttribute?.Description,
                    Type = parameterInfo.ParameterType,
                    In = inclusionType
                });
            }

            return parameters;
        }

        private static InclutionTypes DetermineDefaultInclusionType(ParameterInfo param, string template)
        {
            if (template.Contains("{" + param.Name + "}") || template.Contains("{" + param.Name + ":"))
            {
                return InclutionTypes.Path;
            }

            if (param.ParameterType.IsClass && param.ParameterType != typeof(string))
            {
                return InclutionTypes.Body;
            }

            return InclutionTypes.Query;
        }

        private static void MapMethodToEndpoint<TService>(IEndpointRouteBuilder endpoints, MethodInfo method, IRoutePrefix routePrefix)
            where TService : class
        {
            var verbAttribute = method.GetCustomAttribute<VerbAttribute>();
            var httpMethod = GetHttpMethod(method, verbAttribute);
            var routeTemplate = BuildRouteTemplate(method, routePrefix, verbAttribute);

            var routeBuilder = MapEndpointByVerb<TService>(endpoints, method, httpMethod, routeTemplate);

            ApplyEndpointMetadata(routeBuilder, method);
            ApplyParameterMetadata(routeBuilder, method, routeTemplate);
            ApplyCustomAttributes(routeBuilder, method);
            ApplyEndpointRoutingPolicies(routeBuilder, method, endpoints.ServiceProvider);
        }

        private static void ApplyCustomAttributes(RouteHandlerBuilder routeBuilder, MethodInfo method)
        {
            // Apply ICreateImplementationAttribute metadata
            var attributeCreators = method.GetCustomAttributes().OfType<ICreateImplementationAttribute>();
            foreach (var creator in attributeCreators)
            {
                // Store the attribute creator as metadata for potential use by filters/middleware
                routeBuilder.WithMetadata(creator);
            }

            // Apply interface-level custom attributes
            var interfaceAttributeCreators = method.DeclaringType?.GetCustomAttributes().OfType<ICreateImplementationAttribute>();
            if (interfaceAttributeCreators != null)
            {
                foreach (var creator in interfaceAttributeCreators)
                {
                    routeBuilder.WithMetadata(creator);
                }
            }

            // Apply version attribute metadata
            var versionAttribute = method.DeclaringType?.GetCustomAttribute<VersionAttribute>();
            if (versionAttribute != null)
            {
                routeBuilder.WithMetadata(versionAttribute);
            }

            // Apply throttling metadata
            var throttlingAttribute = method.GetCustomAttribute<ThrottlingAttribute>() 
                ?? method.DeclaringType?.GetCustomAttribute<ThrottlingAttribute>();
            if (throttlingAttribute != null)
            {
                routeBuilder.WithMetadata(throttlingAttribute);
            }
        }

        private static void ApplyEndpointRoutingPolicies(RouteHandlerBuilder routeBuilder, MethodInfo method, IServiceProvider serviceProvider)
        {
            // Collect policy names from method and interface level attributes
            var policyNames = new List<string>();

            // Get method-level policy attributes
            var methodPolicyAttributes = method.GetCustomAttributes<EndpointRoutingPolicyAttribute>();
            foreach (var attr in methodPolicyAttributes)
            {
                if (!string.IsNullOrWhiteSpace(attr.PolicyName))
                {
                    policyNames.Add(attr.PolicyName);
                }
            }

            // Get interface-level policy attributes
            var interfacePolicyAttributes = method.DeclaringType?.GetCustomAttributes<EndpointRoutingPolicyAttribute>();
            if (interfacePolicyAttributes != null)
            {
                foreach (var attr in interfacePolicyAttributes)
                {
                    if (!string.IsNullOrWhiteSpace(attr.PolicyName))
                    {
                        policyNames.Add(attr.PolicyName);
                    }
                }
            }

            if (!policyNames.Any()) return;

            // Get all registered IEndpointRoutingPolicy implementations
            var policies = serviceProvider.GetServices<IEndpointRoutingPolicy>();

            // Apply matching policies
            foreach (var policyName in policyNames.Distinct())
            {
                var matchingPolicy = policies.FirstOrDefault(p => 
                    string.Equals(p.PolicyName, policyName, StringComparison.OrdinalIgnoreCase));

                if (matchingPolicy != null)
                {
                    // Apply the main policy configuration
                    matchingPolicy.ApplyPolicyAsync(routeBuilder);

                    // Apply OpenAPI settings if provided
                    ApplyOpenApiPolicySettings(routeBuilder, matchingPolicy);

                    // Store policy for response header processing
                    routeBuilder.WithMetadata(matchingPolicy);
                }
            }
        }

        private static void ApplyOpenApiPolicySettings(RouteHandlerBuilder routeBuilder, IEndpointRoutingPolicy policy)
        {
            var openApiSettings = policy.GetOpenApiSettings();
            if (openApiSettings == null) return;

#if NET7_0_OR_GREATER
            // Apply summary and description
            if (!string.IsNullOrWhiteSpace(openApiSettings.Summary))
            {
                routeBuilder.WithSummary(openApiSettings.Summary);
            }

            if (!string.IsNullOrWhiteSpace(openApiSettings.Description))
            {
                routeBuilder.WithDescription(openApiSettings.Description);
            }
#endif

            // Apply tags
            if (openApiSettings.Tags != null && openApiSettings.Tags.Length > 0)
            {
                routeBuilder.WithTags(openApiSettings.Tags);
            }

            // Apply deprecation
            if (openApiSettings.IsDeprecated)
            {
                routeBuilder.WithMetadata(new ObsoleteAttribute(openApiSettings.DeprecationMessage ?? "This endpoint is deprecated."));
            }

            // Apply additional responses
            if (openApiSettings.AdditionalResponses != null)
            {
                foreach (var response in openApiSettings.AdditionalResponses)
                {
                    if (response.Value.ResponseType != null)
                    {
                        routeBuilder.Produces(response.Key, response.Value.ResponseType, response.Value.ContentType ?? "application/json");
                    }
                    else
                    {
                        routeBuilder.Produces(response.Key);
                    }
                }
            }

            // Apply custom metadata
            if (openApiSettings.CustomMetadata != null)
            {
                foreach (var metadata in openApiSettings.CustomMetadata)
                {
                    if (metadata != null)
                    {
                        routeBuilder.WithMetadata(metadata);
                    }
                }
            }

            // Apply operation ID
            if (!string.IsNullOrWhiteSpace(openApiSettings.OperationId))
            {
                routeBuilder.WithName(openApiSettings.OperationId);
            }

            // Apply additional content types
            if (openApiSettings.AdditionalContentTypes != null && openApiSettings.AdditionalContentTypes.Length > 0)
            {
                foreach (var contentType in openApiSettings.AdditionalContentTypes)
                {
                    routeBuilder.Accepts(typeof(object), contentType);
                }
            }
        }

        private static RouteHandlerBuilder MapEndpointByVerb<TService>(
            IEndpointRouteBuilder endpoints,
            MethodInfo method,
            string httpMethod,
            string routeTemplate)
            where TService : class
        {
            var delegateHandler = CreateEndpointDelegate<TService>(method);

            return httpMethod.ToUpperInvariant() switch
            {
                "GET" => endpoints.MapGet(routeTemplate, delegateHandler),
                "POST" => endpoints.MapPost(routeTemplate, delegateHandler),
                "PUT" => endpoints.MapPut(routeTemplate, delegateHandler),
                "DELETE" => endpoints.MapDelete(routeTemplate, delegateHandler),
#if NET7_0_OR_GREATER
                "PATCH" => endpoints.MapPatch(routeTemplate, delegateHandler),
#endif
                _ => endpoints.MapMethods(routeTemplate, new[] { httpMethod }, delegateHandler)
            };
        }

        private static Delegate CreateEndpointDelegate<TService>(MethodInfo method)
            where TService : class
        {
            var parameters = method.GetParameters();
            var returnType = method.ReturnType;
            var isAsync = typeof(Task).IsAssignableFrom(returnType);
            var hasReturnValue = returnType != typeof(void) && returnType != typeof(Task);
            var actionName = GetActionName(method.Name);

            return async (HttpContext context) =>
            {
                var serviceProvider = context.RequestServices;
                var service = serviceProvider.GetRequiredService<TService>();
                var action = GetAction<TService>(actionName);
                var locator = new Locator(serviceProvider);

                try
                {
                    // Initialize request state
                    context.Request.InitializeState();
                    var state = context.Request.GetState();
                    state.SetState("controllerName", typeof(TService).FullName);
                    state.SetState("action", actionName);

                    // Check throttling
                    var wait = action.Throttler?.IsThrottled(actionName, typeof(TService).FullName, typeof(TService).Assembly.FullName);
                    if (wait != null)
                    {
                        context.Response.Headers["Retry-After"] = TimeSpan.FromMilliseconds(wait.Value).TotalSeconds.ToString("F0");
                        return Results.StatusCode(429);
                    }

                    // Resolve parameters
                    var args = await ResolveParametersAsync(context, method, parameters, action);

                    // Execute header handlers (get headers)
                    ExecuteHeaderHandlersGet(context, action, locator);

                    // Execute input interceptors
                    var interceptResult = await ExecuteInterceptorsAsync(context, action, args, locator);
                    if (interceptResult != null)
                    {
                        return interceptResult;
                    }

                    // Execute service initializers
                    ExecuteInitializers(service, action, state, args);

                    // Set service extensions context
                    SetupServiceExtensions(service, context);

                    // Invoke the method
                    object result;
                    if (isAsync)
                    {
                        var task = (Task)method.Invoke(service, args);
                        await task.ConfigureAwait(false);

                        if (hasReturnValue && returnType.IsGenericType)
                        {
                            result = GetTaskResult(task, returnType);
                        }
                        else
                        {
                            result = null;
                        }
                    }
                    else
                    {
                        result = method.Invoke(service, args);
                    }

                    // Execute output interceptors
                    result = await ExecuteOutputInterceptorsAsync(context, action, result, locator);

                    var statusCode = action.DefaultResponseCode ?? GetSuccessStatusCode(method);
                    return CreateResponse(context, statusCode, result, service, action, locator);
                }
                catch (TargetInvocationException ex) when (ex.InnerException != null)
                {
                    return HandleException(context, ex.InnerException, action, serviceProvider);
                }
                catch (OperationAbortedException ex)
                {
                    return Results.StatusCode((int)ex.StatusCode);
                }
                catch (ThrottledRequestException ex)
                {
                    context.Response.Headers["Retry-After"] = TimeSpan.FromMilliseconds(ex.WaitValue).TotalSeconds.ToString("F0");
                    return Results.StatusCode(429);
                }
                catch (Exception ex)
                {
                    return HandleException(context, ex, action, serviceProvider);
                }
                finally
                {
                    context.Request.EndState();
                }
            };
        }

        private static void SetupServiceExtensions<TService>(TService service, HttpContext context)
        {
            var serviceExtensions = service as IServiceExtensions;
            serviceExtensions?.SetResponseHeaderCollection(new Dictionary<string, string>());
        }

        private static void ExecuteInitializers<TService>(TService service, EndpointActionWrapper action, StateDictionary state, object[] parameters)
        {
            var initializableService = service as IInitializableService;
            if (initializableService != null && action.Initializers != null)
            {
                foreach (var initializer in action.Initializers)
                {
                    initializer?.Initialize(initializableService, state, parameters);
                }
            }
        }

        private static void ExecuteHeaderHandlersGet(HttpContext context, EndpointActionWrapper action, IServiceLocator locator)
        {
            if (action.CustomHandlers == null) return;

            var handlers = new List<IHeaderHandler>();
            foreach (var inspector in action.CustomHandlers)
            {
                handlers.AddRange(inspector.GetHandlers(locator));
            }

            foreach (var handler in handlers.OrderBy(h => h.ProcessingOrder))
            {
                handler.GetServiceHeader(context.Request.Headers);
            }
        }

        private static void ExecuteHeaderHandlersSet(HttpContext context, EndpointActionWrapper action, IServiceLocator locator)
        {
            if (action.CustomHandlers == null) return;

            var handlers = new List<IHeaderHandler>();
            foreach (var inspector in action.CustomHandlers)
            {
                handlers.AddRange(inspector.GetHandlers(locator));
            }

            foreach (var handler in handlers.OrderBy(h => h.ProcessingOrder))
            {
                handler.SetServiceHeaders(context.Response.Headers);
            }
        }

        private static async Task<IResult> ExecuteInterceptorsAsync(HttpContext context, EndpointActionWrapper action, object[] args, IServiceLocator locator)
        {
            if (action.Interceptors == null || action.Interceptors.Length == 0) return null;

            var state = context.Request.GetState();
            foreach (var interceptorAttr in action.Interceptors)
            {
                var interceptor = interceptorAttr.GetInterceptor(locator);
                
                // Sync interception
                bool cancel = interceptor.Intercept(args, state, out string cancellationMessage, out HttpStatusCode statusCode);
                if (cancel)
                {
                    return Results.StatusCode((int)statusCode);
                }

                // Async interception
                var asyncResult = await interceptor.InterceptAsync(args, state);
                if (asyncResult.Cancel)
                {
                    return Results.StatusCode((int)asyncResult.StatusCode);
                }
            }

            return null;
        }

        private static async Task<object> ExecuteOutputInterceptorsAsync(HttpContext context, EndpointActionWrapper action, object result, IServiceLocator locator)
        {
            if (action.Interceptors == null || action.Interceptors.Length == 0) return result;

            var state = context.Request.GetState();
            foreach (var interceptorAttr in action.Interceptors)
            {
                var interceptor = interceptorAttr.GetInterceptor(locator);
                result = interceptor.Intercept(result, state);
                result = await interceptor.InterceptAsync(result, state);
            }

            return result;
        }

        private static EndpointActionWrapper GetAction<TService>(string actionName)
        {
            if (!_actionCache.TryGetValue(typeof(TService), out var actions))
                throw new InvalidOperationException($"Service type {typeof(TService).FullName} not initialized");

            if (!actions.TryGetValue(actionName, out var action))
                throw new InvalidOperationException($"Action {actionName} not found for service {typeof(TService).FullName}");

            return action;
        }

        private static string GetActionName(string methodName)
        {
            return methodName.EndsWith("Async") ? methodName.Substring(0, methodName.Length - 5) : methodName;
        }

        private static object GetTaskResult(Task task, Type returnType)
        {
            var resultProperty = returnType.GetProperty("Result");
            return resultProperty?.GetValue(task);
        }

        private static async Task<object[]> ResolveParametersAsync(HttpContext context, MethodInfo method, ParameterInfo[] parameters, EndpointActionWrapper action)
        {
            var args = new object[parameters.Length];

            for (int i = 0; i < parameters.Length; i++)
            {
                var param = parameters[i];
                var wrapper = action.Parameters[i];
                
                args[i] = await ResolveParameterValueAsync(context, param, wrapper);
            }

            return args;
        }

        private static async Task<object> ResolveParameterValueAsync(HttpContext context, ParameterInfo param, EndpointParameterWrapper wrapper)
        {
            var paramType = param.ParameterType;
            var paramName = param.Name;
            // Use the effective name (custom name if specified, otherwise parameter name) for query and header lookups
            var effectiveName = wrapper.EffectiveName;

            switch (wrapper.In)
            {
                case InclutionTypes.Path:
                    return ConvertRouteValue(context.Request.RouteValues[paramName], paramType);

                case InclutionTypes.Query:
                    var queryValue = context.Request.Query[effectiveName].FirstOrDefault();
                    return ConvertValue(queryValue, paramType);

                case InclutionTypes.Header:
                    var headerValue = context.Request.Headers[effectiveName].FirstOrDefault();
                    return ConvertValue(headerValue, paramType);

                case InclutionTypes.Body:
                    if (context.Request.ContentLength > 0 || context.Request.ContentType != null)
                    {
                        try
                        {
                            return await context.Request.ReadFromJsonAsync(paramType);
                        }
                        catch
                        {
                            return GetDefaultValue(paramType);
                        }
                    }
                    return GetDefaultValue(paramType);

                default:
                    return GetDefaultValue(paramType);
            }
        }

        private static InclutionTypes DetermineDefaultInclusionType(ParameterInfo param, MethodInfo method)
        {
            var template = method.GetCustomAttribute<VerbAttribute>()?.Route ?? "";

            if (template.Contains("{" + param.Name + "}") || template.Contains("{" + param.Name + ":"))
            {
                return InclutionTypes.Path;
            }

            if (param.ParameterType.IsClass && param.ParameterType != typeof(string))
            {
                return InclutionTypes.Body;
            }

            return InclutionTypes.Query;
        }

        private static async Task<object> ResolveParameterValueAsync(HttpContext context, ParameterInfo param, InclutionTypes inclusionType)
        {
            var paramType = param.ParameterType;
            var paramName = param.Name;

            switch (inclusionType)
            {
                case InclutionTypes.Path:
                    return ConvertRouteValue(context.Request.RouteValues[paramName], paramType);

                case InclutionTypes.Query:
                    var queryValue = context.Request.Query[paramName].FirstOrDefault();
                    return ConvertValue(queryValue, paramType);

                case InclutionTypes.Header:
                    var headerValue = context.Request.Headers[paramName].FirstOrDefault();
                    return ConvertValue(headerValue, paramType);

                case InclutionTypes.Body:
                    if (context.Request.ContentLength > 0 || context.Request.ContentType != null)
                    {
                        try
                        {
                            return await context.Request.ReadFromJsonAsync(paramType);
                        }
                        catch
                        {
                            return GetDefaultValue(paramType);
                        }
                    }
                    return GetDefaultValue(paramType);

                default:
                    return GetDefaultValue(paramType);
            }
        }

        private static object ConvertRouteValue(object value, Type targetType)
        {
            if (value == null) return GetDefaultValue(targetType);

            if (targetType == typeof(string)) return value.ToString();

            try
            {
                var underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;
                return Convert.ChangeType(value.ToString(), underlyingType);
            }
            catch
            {
                return GetDefaultValue(targetType);
            }
        }

        private static object ConvertValue(string value, Type targetType)
        {
            if (string.IsNullOrEmpty(value)) return GetDefaultValue(targetType);

            if (targetType == typeof(string)) return value;

            try
            {
                var underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;
                return Convert.ChangeType(value, underlyingType);
            }
            catch
            {
                return GetDefaultValue(targetType);
            }
        }

        private static object GetDefaultValue(Type type)
        {
            return type.IsValueType ? Activator.CreateInstance(type) : null;
        }

        private static HttpStatusCode GetSuccessStatusCode(MethodInfo method)
        {
            var statusCodeAttribute = method.GetCustomAttribute<SuccessStatusCodeAttribute>();
            if (statusCodeAttribute != null)
            {
                return statusCodeAttribute.StatusCode;
            }

            var returnType = method.ReturnType;
            if (returnType == typeof(void) || returnType == typeof(Task))
            {
                return HttpStatusCode.NoContent;
            }

            return HttpStatusCode.OK;
        }

        private static IResult CreateResponse<TService>(HttpContext context, HttpStatusCode statusCode, object result, TService service, EndpointActionWrapper action, IServiceLocator locator)
        {
            // Set standard headers
            if (ServiceFactory._includeStardustVersion)
            {
                var version = $"{typeof(GetAttribute).Assembly.GetName().Version.Major}.{typeof(GetAttribute).Assembly.GetName().Version.Minor}";
                context.Response.Headers["x-interstellarVersion"] = version;
            }

            // Set action ID header
            var actionId = context.Request.ActionId();
            if (!string.IsNullOrWhiteSpace(actionId))
            {
                context.Response.Headers[ExtensionsFactory.ActionIdName] = actionId;
            }

            // Set service extension headers
            SetServiceExtensionHeaders(context, service);

            // Execute header handlers (set headers)
            ExecuteHeaderHandlersSet(context, action, locator);

            // Apply policy response headers
            ApplyPolicyResponseHeaders(context);

            if (result == null)
            {
                return Results.StatusCode((int)statusCode);
            }

            return Results.Json(result, statusCode: (int)statusCode);
        }

        private static void ApplyPolicyResponseHeaders(HttpContext context)
        {
            // Get policies from endpoint metadata
            var endpoint = context.GetEndpoint();
            var policies = endpoint?.Metadata.GetOrderedMetadata<IEndpointRoutingPolicy>();

            if (policies == null || policies.Count == 0) return;

            var dynamicHeaders = new Dictionary<string, string>();

            foreach (var policy in policies)
            {
                // Apply static response headers
                var staticHeaders = policy.GetResponseHeaders();
                if (staticHeaders != null)
                {
                    foreach (var header in staticHeaders)
                    {
                        try
                        {
                            var sanitizedValue = header.Value?.Replace("\r", "")?.Replace("\n", "")?.Replace("%0a", "")?.Replace("%0d", "");
                            context.Response.Headers[header.Key] = sanitizedValue;
                            dynamicHeaders[header.Key] = sanitizedValue;
                        }
                        catch
                        {
                            // Ignore header setting errors
                        }
                    }
                }

                // Allow policy to modify headers dynamically
                policy.OnBeforeResponse(context, dynamicHeaders);
            }

            // Apply any dynamically modified headers
            foreach (var header in dynamicHeaders)
            {
                try
                {
                    var sanitizedValue = header.Value?.Replace("\r", "")?.Replace("\n", "")?.Replace("%0a", "")?.Replace("%0d", "");
                    context.Response.Headers[header.Key] = sanitizedValue;
                }
                catch
                {
                    // Ignore header setting errors
                }
            }
        }

        private static void SetServiceExtensionHeaders<TService>(HttpContext context, TService service)
        {
            var serviceExtensions = service as IServiceExtensions;
            var headers = serviceExtensions?.GetHeaders();
            if (headers != null)
            {
                foreach (var header in headers)
                {
                    try
                    {
                        var sanitizedValue = header.Value?.Replace("\r", "")?.Replace("\n", "")?.Replace("%0a", "")?.Replace("%0d", "");
                        context.Response.Headers[header.Key] = sanitizedValue;
                    }
                    catch
                    {
                        // Ignore header setting errors
                    }
                }
            }
        }

        private static IResult HandleException(HttpContext context, Exception ex, EndpointActionWrapper action, IServiceProvider serviceProvider)
        {
            // Try custom error handler first
            var errorHandler = GetErrorHandler(action, serviceProvider);
            if (errorHandler != null)
            {
                var customResult = TryCustomErrorHandler(ex, errorHandler);
                if (customResult != null)
                {
                    return customResult;
                }
            }

            // Default exception handling
            if (ex is UnauthorizedAccessException)
            {
                return Results.Unauthorized();
            }

            if (ex is KeyNotFoundException || ex is IndexOutOfRangeException)
            {
                return Results.NotFound(new { Message = ex.Message });
            }

            if (ex is NotImplementedException)
            {
                return Results.StatusCode((int)HttpStatusCode.NotImplemented);
            }

            return Results.Problem(ex.Message, statusCode: (int)HttpStatusCode.InternalServerError);
        }

        private static IErrorHandler GetErrorHandler(EndpointActionWrapper action, IServiceProvider serviceProvider)
        {
            var handler = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetService<IErrorHandler>(serviceProvider);
            var attributeHandler = action.ErrorHandlerAttribute?.ErrorHandler(serviceProvider);

            if (handler != null && attributeHandler != null)
            {
                return new AggregateHandler(handler, attributeHandler);
            }

            return handler ?? attributeHandler;
        }

        private static IResult TryCustomErrorHandler(Exception ex, IErrorHandler errorHandler)
        {
            // The error handler interface is designed for MVC, so we can't directly use it
            // But we can check if it wants to handle specific exception types
            return null;
        }

        private static string GetHttpMethod(MethodInfo method, VerbAttribute verbAttribute)
        {
            if (verbAttribute != null)
            {
                return verbAttribute.Verb ?? "GET";
            }

            var attributes = method.GetCustomAttributes(true);

            if (attributes.OfType<GetAttribute>().Any()) return "GET";
            if (attributes.OfType<PostAttribute>().Any()) return "POST";
            if (attributes.OfType<PutAttribute>().Any()) return "PUT";
            if (attributes.OfType<DeleteAttribute>().Any()) return "DELETE";
            if (attributes.OfType<PatchAttribute>().Any()) return "PATCH";

            return "GET";
        }

        private static string BuildRouteTemplate(MethodInfo method, IRoutePrefix routePrefix, VerbAttribute verbAttribute)
        {
#pragma warning disable CS0618 // Type or member is obsolete
            var methodRoute = verbAttribute?.Route ?? method.GetCustomAttribute<IRouteAttribute>()?.Template ?? "";
#pragma warning restore CS0618 // Type or member is obsolete

            var prefix = routePrefix?.Prefix ?? "";
#pragma warning disable CS0612 // Type or member is obsolete
            if (routePrefix?.IncludeTypeName == true)
#pragma warning restore CS0612 // Type or member is obsolete
            {
                var interfaceType = method.DeclaringType;
                var typeName = interfaceType.GetGenericArguments().FirstOrDefault()?.Name.ToLower()
                    ?? interfaceType.GetInterfaces().FirstOrDefault()?.GetGenericArguments().FirstOrDefault()?.Name.ToLower()
                    ?? interfaceType.Name.TrimStart('I').ToLower();
                prefix = string.IsNullOrEmpty(prefix) ? typeName : $"{prefix}/{typeName}";
            }

            if (string.IsNullOrEmpty(prefix))
            {
                return string.IsNullOrEmpty(methodRoute) ? method.Name : methodRoute;
            }

            if (string.IsNullOrEmpty(methodRoute))
            {
                return prefix;
            }

            return $"{prefix}/{methodRoute}".Replace("//", "/");
        }

        private static IRoutePrefix GetRoutePrefix(Type interfaceType)
        {
#pragma warning disable CS0618 // Type or member is obsolete
            return interfaceType.GetCustomAttribute<ApiAttribute>()
                ?? interfaceType.GetInterfaces().FirstOrDefault()?.GetCustomAttribute<ApiAttribute>()
                ?? (IRoutePrefix)interfaceType.GetCustomAttribute<IRoutePrefixAttribute>()
                ?? interfaceType.GetInterfaces().FirstOrDefault()?.GetCustomAttribute<IRoutePrefixAttribute>();
#pragma warning restore CS0618 // Type or member is obsolete
        }

        private static void ApplyEndpointMetadata(RouteHandlerBuilder routeBuilder, MethodInfo method)
        {
            ApplyAuthorizationAttributes(routeBuilder, method);
            ApplyProducesMetadata(routeBuilder, method);
            ApplyTagsAndDescription(routeBuilder, method);
            
            // Set operation name for OpenAPI
            var operationName = method.Name.EndsWith("Async") 
                ? method.Name.Substring(0, method.Name.Length - 5) 
                : method.Name;
            routeBuilder.WithName(operationName);
        }

        private static void ApplyAuthorizationAttributes(RouteHandlerBuilder routeBuilder, MethodInfo method)
        {
            var authorizeAttributes = method.GetCustomAttributes<AuthorizeWrapperAttribute>().ToList();
            var classAuthorizeAttributes = method.DeclaringType?.GetCustomAttributes<AuthorizeWrapperAttribute>();

            if (classAuthorizeAttributes != null)
            {
                authorizeAttributes.AddRange(classAuthorizeAttributes);
            }

            if (authorizeAttributes.Any())
            {
                routeBuilder.RequireAuthorization();
            }
            else
            {
                routeBuilder.AllowAnonymous();
            }
        }

        private static void ApplyProducesMetadata(RouteHandlerBuilder routeBuilder, MethodInfo method)
        {
            var returnType = method.ReturnType;

            if (typeof(Task).IsAssignableFrom(returnType) && returnType.IsGenericType)
            {
                returnType = returnType.GetGenericArguments().FirstOrDefault() ?? typeof(void);
            }

            var statusCodeAttribute = method.GetCustomAttribute<SuccessStatusCodeAttribute>();
            var statusCode = statusCodeAttribute?.StatusCode ?? HttpStatusCode.OK;

            if (returnType != typeof(void))
            {
                routeBuilder.Produces((int)statusCode, returnType, "application/json");
            }
            else
            {
                routeBuilder.Produces((int)HttpStatusCode.NoContent);
            }

            // Add throttling response
            var throttlingAttribute = method.GetCustomAttribute<ThrottlingAttribute>() 
                ?? method.DeclaringType?.GetCustomAttribute<ThrottlingAttribute>();
            if (throttlingAttribute != null)
            {
                routeBuilder.Produces(429);
            }

            routeBuilder.ProducesProblem((int)HttpStatusCode.InternalServerError);
            routeBuilder.Produces((int)HttpStatusCode.Unauthorized);
            routeBuilder.Produces((int)HttpStatusCode.NotFound);
        }

        private static void ApplyParameterMetadata(RouteHandlerBuilder routeBuilder, MethodInfo method, string routeTemplate)
        {
            var parameters = method.GetParameters();
            Type bodyType = null;
            
            foreach (var param in parameters)
            {
                var inAttribute = param.GetCustomAttribute<InAttribute>();
                var inclusionType = inAttribute?.InclutionType ?? DetermineDefaultInclusionType(param, method);
                var customName = inAttribute?.Name;
                var description = inAttribute?.Description;
                
                if (inclusionType == InclutionTypes.Body)
                {
                    bodyType = param.ParameterType;
                }
                
                // Add parameter metadata for OpenAPI documentation
                var parameterMetadata = new EndpointParameterMetadata(
                    param.Name,
                    customName,
                    description,
                    param.ParameterType,
                    inclusionType,
                    !param.ParameterType.IsValueType || Nullable.GetUnderlyingType(param.ParameterType) != null
                );
                routeBuilder.WithMetadata(parameterMetadata);
            }
            
            // Add Accepts metadata for body parameters
            if (bodyType != null)
            {
                routeBuilder.Accepts(bodyType, "application/json");
            }
        }

        private static void ApplyTagsAndDescription(RouteHandlerBuilder routeBuilder, MethodInfo method)
        {
            var serviceDescription = method.GetCustomAttribute<ServiceDescriptionAttribute>();
            var interfaceDescription = method.DeclaringType?.GetCustomAttribute<ServiceDescriptionAttribute>();

            var tags = new List<string>();

            if (!string.IsNullOrWhiteSpace(interfaceDescription?.Tags))
            {
                tags.AddRange(interfaceDescription.Tags.Split(',').Select(t => t.Trim()));
            }

            if (!string.IsNullOrWhiteSpace(serviceDescription?.Tags))
            {
                tags.AddRange(serviceDescription.Tags.Split(',').Select(t => t.Trim()));
            }

            if (tags.Any())
            {
                routeBuilder.WithTags(tags.Distinct().ToArray());
            }
            else
            {
                var interfaceName = method.DeclaringType?.Name.TrimStart('I') ?? "Api";
                routeBuilder.WithTags(interfaceName);
            }

#if NET7_0_OR_GREATER
            if (!string.IsNullOrWhiteSpace(serviceDescription?.Summary))
            {
                routeBuilder.WithSummary(serviceDescription.Summary);
            }

            if (!string.IsNullOrWhiteSpace(serviceDescription?.Description))
            {
                routeBuilder.WithDescription(serviceDescription.Description);
            }
#endif

            var obsoleteAttribute = method.GetCustomAttribute<ObsoleteAttribute>();
            if (obsoleteAttribute != null || (serviceDescription?.IsDeprecated ?? false))
            {
                routeBuilder.WithMetadata(new ObsoleteAttribute(obsoleteAttribute?.Message ?? "This endpoint is deprecated."));
            }
        }
    }

    /// <summary>
    /// Wrapper for endpoint action metadata including interceptors, throttling, etc.
    /// </summary>
    internal sealed class EndpointActionWrapper
    {
        public string Name { get; set; }
        public MethodInfo Method { get; set; }
        public Type ReturnType { get; set; }
        public List<EndpointParameterWrapper> Parameters { get; set; }
        public HttpStatusCode? DefaultResponseCode { get; set; }
        public InputInterceptorAttribute[] Interceptors { get; set; }
        public List<ServiceInitializerAttribute> Initializers { get; set; }
        public List<IHeaderInspector> CustomHandlers { get; set; }
        public IThrottlingManager Throttler { get; set; }
        public ErrorHandlerAttribute ErrorHandlerAttribute { get; set; }
    }

    /// <summary>
    /// Wrapper for endpoint parameter information.
    /// </summary>
    internal sealed class EndpointParameterWrapper
    {
        public string Name { get; set; }
        
        /// <summary>
        /// Gets or sets the custom name to use for the parameter in requests/responses.
        /// When null or empty, the Name property will be used.
        /// </summary>
        public string CustomName { get; set; }
        
        /// <summary>
        /// Gets or sets the description for the parameter.
        /// </summary>
        public string Description { get; set; }
        
        /// <summary>
        /// Gets the effective name to use for the parameter (CustomName if set, otherwise Name).
        /// </summary>
        public string EffectiveName => !string.IsNullOrWhiteSpace(CustomName) ? CustomName : Name;
        
        public Type Type { get; set; }
        public InclutionTypes In { get; set; }
    }

    /// <summary>
    /// Metadata class for endpoint parameters to support OpenAPI documentation.
    /// </summary>
    internal sealed class EndpointParameterMetadata : IParameterBindingMetadata
    {
        public EndpointParameterMetadata(string name, string customName, string description, Type parameterType, InclutionTypes source, bool isOptional)
        {
            Name = name;
            CustomName = customName;
            Description = description;
            ParameterType = parameterType;
            Source = source;
            IsOptional = isOptional;
        }

        public string Name { get; }
        
        /// <summary>
        /// Gets the custom name to use for the parameter in requests/responses.
        /// </summary>
        public string CustomName { get; }
        
        /// <summary>
        /// Gets the effective name to use for the parameter (CustomName if set, otherwise Name).
        /// </summary>
        public string EffectiveName => !string.IsNullOrWhiteSpace(CustomName) ? CustomName : Name;
        
        /// <summary>
        /// Gets the description for the parameter.
        /// </summary>
        public string Description { get; }
        
        public Type ParameterType { get; }
        public InclutionTypes Source { get; }
        public bool IsOptional { get; }
    }
    
    /// <summary>
    /// Interface marker for parameter binding metadata.
    /// </summary>
    internal interface IParameterBindingMetadata
    {
        string Name { get; }
        string CustomName { get; }
        string EffectiveName { get; }
        string Description { get; }
        Type ParameterType { get; }
        InclutionTypes Source { get; }
        bool IsOptional { get; }
    }
}

#endif
