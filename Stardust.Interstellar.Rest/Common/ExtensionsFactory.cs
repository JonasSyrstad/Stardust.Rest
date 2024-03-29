using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using Stardust.Interstellar.Rest.Annotations;
using Stardust.Interstellar.Rest.Annotations.Messaging;
using Stardust.Interstellar.Rest.Client;
using Stardust.Interstellar.Rest.Extensions;

namespace Stardust.Interstellar.Rest.Common
{
    public static class ExtensionsFactory
    {
        public static StateDictionary GetState(this ResultWrapper result)
        {
            var actionId = result.ActionId;
            return StateHelper.InitializeState(actionId);
        }


        private static Type xmlSerializer;




        public static string GetServiceTemplate(MethodInfo methodInfo, IServiceProvider serviceLocator)
        {
            var template = serviceLocator.GetService<IRouteTemplateResolver>()?.GetTemplate(methodInfo);
            if (!String.IsNullOrWhiteSpace(template)) return template;
            var verbAttribute = methodInfo.GetCustomAttribute<VerbAttribute>();
            var templateAttrib = methodInfo.GetCustomAttribute<IRouteAttribute>();
            if (templateAttrib == null)
            {
                return verbAttribute?.Route ?? template ?? "";
            }
            template = templateAttrib.Template;
            return template ?? "";
        }

        public static string GetRouteTemplate(IRoutePrefix templatePrefix, IRoute template, MethodInfo methodInfo, IServiceProvider serviceLocator)
        {
            var interfaceType = methodInfo.DeclaringType;
            var templateResolver = serviceLocator.GetService<IRouteTemplateResolver>();
            var route = templateResolver?.GetTemplate(methodInfo);
            if (!String.IsNullOrWhiteSpace(route)) return route;
            string prefix = "";
            if (templatePrefix != null)
            {
                prefix = templatePrefix.Prefix;
                if (templatePrefix.IncludeTypeName) prefix = prefix + "/" + (interfaceType.GetGenericArguments().Any() ? interfaceType.GetGenericArguments().FirstOrDefault()?.Name.ToLower() : interfaceType.GetInterfaces().FirstOrDefault()?.GetGenericArguments().First().Name.ToLower());

            }
            return (templatePrefix == null ? "" : (prefix + "/") + template.Template).Replace("//", "/");
        }

        public static void BuildParameterInfo(MethodInfo methodInfo, ActionWrapper action, IServiceProvider serviceLocator)
        {

            var parameterHandler = serviceLocator.GetService<IServiceParameterResolver>();
            if (parameterHandler != null)
            {
                var parameters = parameterHandler.ResolveParameters(methodInfo);
                if (parameters != null && parameters.Any())
                {
                    action.Parameters.AddRange(parameters);
                    foreach (var pi in methodInfo.GetParameters())
                    {
                        action.MessageExtesionLevel = pi.GetCustomAttribute<ExtensionLevelAttribute>() == null
                            ? action.MessageExtesionLevel
                            : pi.GetCustomAttribute<ExtensionLevelAttribute>().PropertInjectionLevel;
                    }
                    return;
                }
            }
            foreach (var parameterInfo in methodInfo.GetParameters())
            {
                var @in = parameterInfo.GetCustomAttribute<InAttribute>(true);
                //if (@in == null)
                //{
                //    var fromBody = parameterInfo.GetCustomAttribute<FromBodyAttribute>(true);
                //    if (fromBody != null)
                //        @in = new InAttribute(InclutionTypes.Body);
                //    if (@in == null)
                //    {
                //        var fromUri = parameterInfo.GetCustomAttribute<FromUriAttribute>(true);
                //        if (fromUri != null)
                //            @in = new InAttribute(InclutionTypes.Path);
                //    }

                //}
                action.MessageExtesionLevel = parameterInfo.GetCustomAttribute<ExtensionLevelAttribute>() == null
                           ? action.MessageExtesionLevel
                           : parameterInfo.GetCustomAttribute<ExtensionLevelAttribute>().PropertInjectionLevel;
                action.Parameters.Add(new ParameterWrapper { Name = parameterInfo.Name, Type = parameterInfo.ParameterType, In = @in?.InclutionType ?? InclutionTypes.Body });
            }
        }

        public static List<IHeaderInspector> GetHeaderInspectors(MethodInfo methodInfo, IServiceProvider serviceLocator)
        {
            var inspectors = GetInspectors(methodInfo);
            var headerInspectors = new Locator(serviceLocator).GetServices<IHeaderInspector>();
            var handlers = new List<IHeaderInspector>();
            if (headerInspectors != null) handlers.AddRange(headerInspectors);
            foreach (var inspector in inspectors)
            {
                handlers.Add(inspector);
            }
            return handlers.Where(i => i != null).ToList();
        }

        private static List<IHeaderInspector> GetInspectors(MethodInfo methodInfo)
        {
            var inspectors = methodInfo.GetCustomAttributes().OfType<IHeaderInspector>().ToList();
            var typeInspectors = methodInfo.DeclaringType.GetCustomAttributes().OfType<IHeaderInspector>();
            var enumerable = typeInspectors as IHeaderInspector[] ?? typeInspectors.Where(i => inspectors.All(x => x.GetType() != i.GetType())).ToArray();
            if (enumerable.Any()) inspectors.AddRange(enumerable);
            var assemblyInstpctors = methodInfo.DeclaringType.Assembly.GetCustomAttributes().OfType<IHeaderInspector>().Where(i => inspectors.All(x => x.GetType() != i.GetType())).ToArray();
            if (assemblyInstpctors.Any()) inspectors.AddRange(assemblyInstpctors);
            return inspectors;
        }

        public static List<HttpMethod> GetHttpMethods(List<VerbAttribute> actions, MethodInfo method, IServiceProvider serviceLocator)
        {
            var methodResolver = serviceLocator.GetService<IWebMethodConverter>();
            var methods = new List<HttpMethod>();
            if (methodResolver != null) methods.AddRange(methodResolver.GetHttpMethods(method));
            foreach (var actionHttpMethodProvider in actions)
            {
                methods.Add(new HttpMethod(actionHttpMethodProvider.Verb));
            }
            if (methods.Count == 0) methods.Add(HttpMethod.Get);
            return methods;
        }


        public const string ActionIdName = "sd-ActionId";

        public static void SetXmlSerializer(Type type)
        {
            xmlSerializer = type;
        }
    }

    public class HandlerWrapper : IHeaderInspector
    {
        private readonly IHeaderHandler _headerHandler;

        public HandlerWrapper(IHeaderHandler headerHandler)
        {
            _headerHandler = headerHandler;
        }

        public IHeaderHandler[] GetHandlers(IServiceProvider serviceLocator)
        {
            return new[] { _headerHandler };
        }
    }
}