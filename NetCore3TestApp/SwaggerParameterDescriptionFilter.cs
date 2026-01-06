using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.OpenApi.Models;
using Stardust.Interstellar.Rest.Annotations;
using Swashbuckle.AspNetCore.SwaggerGen;
using System;
using System.Linq;
using System.Reflection;

namespace NetCore3TestApp
{
    /// <summary>
    /// Swagger operation filter that adds parameter descriptions and custom names from InAttribute to generated API documentation.
    /// </summary>
    public class SwaggerParameterDescriptionFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            if (context.MethodInfo == null)
                return;

            // Try to resolve the interface method for proper parameter attribute lookup
            var interfaceMethod = ResolveInterfaceMethod(context.MethodInfo);
            var methodParameters = interfaceMethod?.GetParameters() ?? context.MethodInfo.GetParameters();

            foreach (var methodParam in methodParameters)
            {
                // Check for InAttribute (base class for InQuery, InHeader, etc.)
                var inAttribute = methodParam.GetCustomAttribute<InAttribute>(true);
                if (inAttribute == null)
                    continue;

                // Determine the C# parameter name and the custom name (if any)
                var csharpName = methodParam.Name;
                var customName = inAttribute.Name;
                var effectiveName = !string.IsNullOrWhiteSpace(customName) ? customName : csharpName;
                var description = inAttribute.Description;

                // Find the OpenAPI parameter - check multiple possible names
                // The parameter might be using:
                // 1. The custom name (from FromQueryAttribute.Name or FromHeaderAttribute.Name)
                // 2. The C# parameter name
                // 3. A case-insensitive match
                var openApiParam = FindOpenApiParameter(operation, csharpName, customName, effectiveName);
                
                if (openApiParam != null)
                {
                    // Update the parameter name to use the effective name (custom name if specified)
                    openApiParam.Name = effectiveName;
                    
                    // Set description if available
                    if (!string.IsNullOrWhiteSpace(description))
                    {
                        openApiParam.Description = description;
                    }
                }
                else
                {
                    // Also try to find by matching against API description parameters
                    var apiParam = context.ApiDescription?.ParameterDescriptions?
                        .FirstOrDefault(p => string.Equals(p.Name, csharpName, StringComparison.OrdinalIgnoreCase) ||
                                            string.Equals(p.Name, effectiveName, StringComparison.OrdinalIgnoreCase));
                    
                    if (apiParam != null)
                    {
                        openApiParam = operation.Parameters?.FirstOrDefault(p => 
                            string.Equals(p.Name, apiParam.Name, StringComparison.OrdinalIgnoreCase));
                        
                        if (openApiParam != null)
                        {
                            openApiParam.Name = effectiveName;
                            if (!string.IsNullOrWhiteSpace(description))
                            {
                                openApiParam.Description = description;
                            }
                        }
                    }
                }
            }
            
            // Also check request body parameters
            if (operation.RequestBody != null)
            {
                var bodyParams = methodParameters.Where(p => p.GetCustomAttribute<InBodyAttribute>(true) != null 
                    || p.GetCustomAttribute<InAttribute>(true)?.InclusionType == InclusionTypes.Body);
                
                foreach (var bodyParam in bodyParams)
                {
                    var inAttribute = bodyParam.GetCustomAttribute<InAttribute>(true);
                    if (inAttribute != null && !string.IsNullOrWhiteSpace(inAttribute.Description))
                    {
                        operation.RequestBody.Description = inAttribute.Description;
                        break; // Only one body parameter
                    }
                }
            }
        }

        /// <summary>
        /// Finds an OpenAPI parameter by checking multiple possible names.
        /// </summary>
        private static OpenApiParameter? FindOpenApiParameter(OpenApiOperation operation, string? csharpName, string? customName, string effectiveName)
        {
            if (operation.Parameters == null || operation.Parameters.Count == 0)
                return null;

            // Try exact matches first
            var param = operation.Parameters.FirstOrDefault(p => p.Name == effectiveName);
            if (param != null) return param;

            if (!string.IsNullOrWhiteSpace(customName))
            {
                param = operation.Parameters.FirstOrDefault(p => p.Name == customName);
                if (param != null) return param;
            }

            if (!string.IsNullOrWhiteSpace(csharpName))
            {
                param = operation.Parameters.FirstOrDefault(p => p.Name == csharpName);
                if (param != null) return param;
            }

            // Try case-insensitive matches
            param = operation.Parameters.FirstOrDefault(p => 
                string.Equals(p.Name, effectiveName, StringComparison.OrdinalIgnoreCase));
            if (param != null) return param;

            if (!string.IsNullOrWhiteSpace(customName))
            {
                param = operation.Parameters.FirstOrDefault(p => 
                    string.Equals(p.Name, customName, StringComparison.OrdinalIgnoreCase));
                if (param != null) return param;
            }

            if (!string.IsNullOrWhiteSpace(csharpName))
            {
                param = operation.Parameters.FirstOrDefault(p => 
                    string.Equals(p.Name, csharpName, StringComparison.OrdinalIgnoreCase));
                if (param != null) return param;
            }

            return null;
        }

        /// <summary>
        /// Tries to resolve the interface method from a generated controller method.
        /// </summary>
        private static MethodInfo? ResolveInterfaceMethod(MethodInfo controllerMethod)
        {
            if (controllerMethod == null) return null;
            
            var declaringType = controllerMethod.DeclaringType;
            if (declaringType == null) return null;
            
            // Check if this is a generated controller (inherits from ServiceWrapperBase<T>)
            var baseType = declaringType.BaseType;
            if (baseType == null || !baseType.IsGenericType) return null;
            
            var baseTypeName = baseType.GetGenericTypeDefinition().FullName;
            if (baseTypeName == null || !baseTypeName.Contains("ServiceWrapperBase")) return null;
            
            // Get the interface type from the generic argument
            var interfaceType = baseType.GetGenericArguments().FirstOrDefault();
            if (interfaceType == null) return null;
            
            // Find the matching method on the interface
            var methodName = controllerMethod.Name;
            var controllerParams = controllerMethod.GetParameters();
            var interfaceMethods = interfaceType.GetMethods();
            
            // Try to find method by name and parameter count
            var candidates = interfaceMethods.Where(m => 
                m.Name == methodName || 
                m.Name == methodName + "Async" ||
                (methodName.EndsWith("Async") && m.Name == methodName.Substring(0, methodName.Length - 5)))
                .ToList();

            // If we have multiple candidates, try to match by parameter count
            if (candidates.Count > 1)
            {
                var match = candidates.FirstOrDefault(m => m.GetParameters().Length == controllerParams.Length);
                if (match != null) return match;
            }

            return candidates.FirstOrDefault();
        }
    }
}
