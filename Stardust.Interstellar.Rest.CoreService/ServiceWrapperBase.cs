using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.DependencyInjection;
using Stardust.Interstellar.Rest.Annotations;
using Stardust.Interstellar.Rest.Annotations.Messaging;
using Stardust.Interstellar.Rest.Annotations.Service;
using Stardust.Interstellar.Rest.Common;
using Stardust.Interstellar.Rest.Extensions;

namespace Stardust.Interstellar.Rest.Service
{
    public abstract class ServiceWrapperBase : Controller
    {
        protected internal readonly IServiceProvider _serviceLocator;

        protected ServiceWrapperBase(IServiceProvider serviceLocator)
        {
            _serviceLocator = serviceLocator;
        }
    }
    public abstract class ServiceWrapperBase<T> : ServiceWrapperBase
    {
        private const string ActionWrapperName = "sd-actionWrapperName";
        private const string ActionId = "sd-ActionId";

        public readonly T implementation;


        protected IActionResult CreateResponse<TMessage>(HttpStatusCode statusCode, TMessage message = default(TMessage))
        {

            IActionResult result;
            var action = GetAction();
            foreach (var interceptor in action.Interceptor)
            {
                message = (TMessage)interceptor.GetInterceptor(new Locator(_serviceLocator)).Intercept(message, Request.GetState());
            }
            if (message == null)
            {
                result = new EmptyResult();//Request.CreateResponse(HttpStatusCode.NoContent);
            }
            else
                result = new JsonResult(message) { StatusCode = (int)statusCode };

            SetHeaders(Response);
            Request.EndState();
            return result;
        }

        private void SetHeaders(HttpResponse result)
        {
            SetServiceHeaders(result);
            var action = GetAction();

            var actionId = Request.ActionId();
            if (string.IsNullOrWhiteSpace(actionId))
            {
                if (Request.HttpContext.Items.ContainsKey(ActionId))
                    actionId = Request.HttpContext.Items[ActionId].ToString();
            }
            result.Headers.Add(ExtensionsFactory.ActionIdName, actionId);
            var handler = new List<IHeaderHandler>();
            foreach (var customHandler in action.CustomHandlers)
            {
                handler.AddRange(customHandler.GetHandlers(_serviceLocator));
            }

            foreach (var customHandler in handler.OrderBy(f => f.ProcessingOrder))
            {
                customHandler.SetServiceHeaders(result.Headers);
            }
        }

        private void SetServiceHeaders(HttpResponse result)
        {
            try
            {
                var serviceExtensions = implementation as IServiceExtensions;
                var dictionary = serviceExtensions?.GetHeaders();
                if (dictionary != null)
                {
                    foreach (var headerElement in dictionary)
                    {
                        try
                        {
                            //if (String.Equals(headerElement.Key, "etag", StringComparison.OrdinalIgnoreCase))
                            //    result.Headers.
                            //else if (String.Equals(headerElement.Key, "wetag", StringComparison.OrdinalIgnoreCase))
                            //    result.Headers.ETag = new EntityTagHeaderValue(headerElement.Value, true);
                            //else
                            result.Headers.Add(headerElement.Key, headerElement.Value);
                        }
                        catch (Exception ex)
                        {
                            ServiceProviderExtensions.GetService<ILogger>(_serviceLocator)?.Error(ex);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                try
                {
                    ServiceProviderExtensions.GetService<ILogger>(_serviceLocator)?.Error(ex);
                }
                catch (Exception)
                {
                }
            }
        }

        private ActionWrapper GetAction()
        {
            var actionName = Request.Headers.Where(h => h.Key == ActionWrapperName).Select(h => h.Value).FirstOrDefault().FirstOrDefault();
            if (string.IsNullOrWhiteSpace(actionName)) actionName = GetActionName(this.ControllerContext.ActionDescriptor.ActionName);
            var action = GetActionWrapper(actionName);
            return action;
        }

        protected async Task<IActionResult> CreateResponseAsync<TMessage>(HttpStatusCode statusCode, Task<TMessage> messageTask = null)
        {
            IActionResult result = null;
            try
            {
                await messageTask.ContinueWith(
                    r =>
                        {
                            r.Exception.Flatten().Handle(
                                e =>
                                    {
                                        result = CreateErrorResponse(e);
                                        return true;
                                    });
                            r.Exception.Handle(e => true);
                        }, TaskContinuationOptions.OnlyOnFaulted);
                if (result == null)
                {
                    var message = messageTask.Result;
                    result = await CreateResponseMessage(statusCode, message, result);
                }
                SetHeaders(Response);
                Request.EndState();
                return result;
            }
            catch (Exception ex)
            {
                if (messageTask.Status == TaskStatus.RanToCompletion)
                {
                    var message = messageTask.Result;
                    result = await CreateResponseMessage(statusCode, message, result);
                    SetHeaders(Response);
                    Request.EndState();
                }
                else
                    result = CreateErrorResponse(ex);
                return result;
            }
        }

        private async Task<IActionResult> CreateResponseMessage<TMessage>(HttpStatusCode statusCode, TMessage message, IActionResult result)
        {

            var action = GetAction();
            foreach (var interceptor in action.Interceptor)
            {
                message = (TMessage)interceptor.GetInterceptor(new Locator(_serviceLocator)).Intercept(message, Request.GetState());
                message = (TMessage)await interceptor.GetInterceptor(new Locator(_serviceLocator)).InterceptAsync(message, Request.GetState());
            }
            if (message == null) result = new OkResult();
            else result = new OkObjectResult(message);
            return result;
        }

        protected async Task<IActionResult> CreateResponseVoidAsync(HttpStatusCode statusCode, Task messageTask)
        {
            IActionResult result = null;
            try
            {
                await messageTask.ContinueWith(r => r.Exception.Flatten().Handle(e =>
                {
                    result = CreateErrorResponse(e);
                    return true;
                }), TaskContinuationOptions.OnlyOnFaulted);
                if (result != null)
                    result = new OkResult();
                SetHeaders(Response);
                Request.EndState();
            }
            catch (Exception ex)
            {
                if (messageTask.Status == TaskStatus.RanToCompletion)
                {
                    result = new OkResult();
                    SetHeaders(Response);
                    Request.EndState();
                }
                else
                    result = CreateErrorResponse(ex);
            }
            return result;
        }

        protected IActionResult CreateErrorResponse(Exception ex)
        {
            IActionResult result = null;
            var exception = ex as ThrottledRequestException;
            if (exception != null)
            {
                result = new StatusCodeResult(429);
                Response.Headers.Add("RetryAfter", TimeSpan.FromMilliseconds(exception.WaitValue).TotalMilliseconds.ToString());
            }
            if (result == null)
            {
                var errorHandler = GetErrorHandler();
                result = ConvertExceptionToErrorResult(ex, Response, errorHandler);
            }
            if (result == null) result = new StatusCodeResult((int)HttpStatusCode.InternalServerError);
            SetHeaders(Response);
            Request.EndState();
            return result;
        }

        private IErrorHandler GetErrorHandler()
        {

            var errorHandler = ServiceProviderExtensions.GetService<IErrorHandler>(_serviceLocator);
            if (errorHandler == null) return errorInterceptor;
            if (errorInterceptor != null) return new AggregateHandler(errorHandler, errorInterceptor);

            return errorHandler;
        }

        private IActionResult ConvertExceptionToErrorResult(Exception ex, HttpResponse result, IErrorHandler errorHandler)
        {
            IActionResult response;
            //if (errorHandler != null && errorHandler.OverrideDefaults)
            //{
            //    response = //errorHandler.ConvertToErrorResponse(ex, null);

            //    if (result != null) return response;
            //}
            if (ex is UnauthorizedAccessException) response = new UnauthorizedResult();
            else if (ex is IndexOutOfRangeException || ex is KeyNotFoundException) response = new ObjectResult(new ErrorDescriptor { Message = ex.Message }) { StatusCode = (int)HttpStatusCode.NotFound };
            else if (ex is NotImplementedException)
                response = new StatusCodeResult((int)HttpStatusCode.NotImplemented);// Request.CreateErrorResponse(HttpStatusCode.NotImplemented, ex.Message);
            // else if (errorHandler != null) result = errorHandler.ConvertToErrorResponse(ex, Request);
            else response = StatusCode((int)HttpStatusCode.InternalServerError, ex.Message);
            return response;
        }

        private static ConcurrentDictionary<Type, ConcurrentDictionary<string, ActionWrapper>> cache = new ConcurrentDictionary<Type, ConcurrentDictionary<string, ActionWrapper>>();

        private IErrorHandler errorInterceptor;

        protected ServiceWrapperBase(T implementation, IServiceProvider serviceLocator) : base(serviceLocator)
        {
            this.implementation = implementation;
            InitializeServiceApi(typeof(T));
        }


        protected ParameterWrapper[] GatherParameters(string name, object[] fromWebMethodParameters)
        {
            _name = name;
            GetMessageExtensions();
            var serviceExtensions = implementation as IServiceExtensions;
            serviceExtensions?.SetControllerContext(ControllerContext);
            serviceExtensions?.SetResponseHeaderCollection(new Dictionary<string, string>());
            var wrappers = new List<ParameterWrapper>();
            List<AuthorizeAttribute> auth = null;
            try
            {

                Request.InitializeState();
                var action = GetAction(name);
                var state = Request.GetState();
                var wait = action.Throttler?.IsThrottled(name, implementation.GetType().FullName, implementation.GetType().Assembly.FullName);
                if (wait != null)
                    throw new ThrottledRequestException(wait.Value);

                state.SetState("controller", this);
                state.SetState("controllerName", typeof(T).FullName);
                state.SetState("action", action.Name);
                var i = 0;
                foreach (var parameter in action.Parameters)
                {
                    var val = parameter.In == InclutionTypes.Header
                        ? GetFromHeaders(parameter)
                        : fromWebMethodParameters[i];
                    wrappers.Add(parameter.Create(val));
                    i++;
                }
                this.Request.Headers.Add(ActionWrapperName, GetActionName(name));
                var handler = new List<IHeaderHandler>();
                foreach (var customHandler in action.CustomHandlers)
                {
                    handler.AddRange(customHandler.GetHandlers(_serviceLocator));
                }

                foreach (var customHandler in handler.OrderBy(f => f.ProcessingOrder))
                {
                    customHandler.GetServiceHeader(Request.Headers);
                }
                ExecuteInterceptors(action, wrappers);
                ExecuteInitializers(action, state, wrappers);

            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (UnauthorizedAccessException)
            {
                throw;
            }
            catch (ThrottledRequestException)
            {
                throw;
            }
            catch (Exception ex)
            {

                throw new ParameterReflectionException(string.Format("Unable to gather parameters for {0}", name), ex);
            }
            if (!ModelState.IsValid) throw new InvalidDataException("Invalid input data");
            _wrappers = wrappers;
            return wrappers.ToArray();
        }

        private void ExecuteInitializers(ActionWrapper action, StateDictionary state, IEnumerable<ParameterWrapper> wrappers)
        {
            var service = implementation as IInitializableService;
            if (service != null)
            {
                var parameters = wrappers?.Select(p => p.value).ToArray() ?? new object[] { };
                action.Initializers?.ForEach(init => init?.Initialize(service, state, parameters));
            }
        }

        private void GetMessageExtensions()
        {
            if (typeof(IServiceWithGlobalParameters).IsAssignableFrom(typeof(T)))
            {
                var messageContainer = MessageExtensions.GetCache(_serviceLocator);
                if (messageContainer == null) return;
                using (var memStream = Request.Body)
                {

                    memStream.Position = 0;
                    using (var reader = new StreamReader(memStream))
                    {
                        var msg = reader.ReadToEnd();
                        if (!string.IsNullOrWhiteSpace(msg))
                        {
                            var jobj = JObject.Parse(msg);
                            messageContainer.SetState(jobj);
                        }
                    }
                }
            }
        }

        private void ExecuteInterceptors(ActionWrapper action, List<ParameterWrapper> wrappers)
        {
            if (action.Interceptor != null)
            {
                foreach (var interceptor in action.Interceptor)
                {
                    bool cancel;
                    string cancellationMessage;
                    HttpStatusCode statusCode;
                    cancel = interceptor.GetInterceptor(new Locator(_serviceLocator)).Intercept(wrappers.Select(p => p.value).ToArray(), Request.GetState(), out cancellationMessage, out statusCode);
                    if (cancel) throw new OperationAbortedException(statusCode, cancellationMessage);
                }
            }
        }

        private async Task ExecuteInterceptorsAsync(ActionWrapper action, IEnumerable<ParameterWrapper> wrappers)
        {
            if (action.Interceptor != null)
            {
                foreach (var interceptor in action.Interceptor)
                {
                    var cancel = await interceptor.GetInterceptor(new Locator(_serviceLocator)).InterceptAsync(wrappers.Select(p => p.value).ToArray(), Request.GetState());
                    if (cancel.Cancel) throw new OperationAbortedException(cancel.StatusCode, cancel.CancellationMessage);
                }
            }
        }

        private static ActionWrapper GetAction(string name)
        {
            var actionName = GetActionName(name);
            return GetActionWrapper(actionName);
        }

        private static ActionWrapper GetActionWrapper(string actionName)
        {
            ConcurrentDictionary<string, ActionWrapper> item;
            if (!cache.TryGetValue(typeof(T), out item)) throw new InvalidOperationException("Invalid interface type");
            ActionWrapper action;
            if (!item.TryGetValue(actionName, out action)) throw new InvalidOperationException("Invalid action");
            return action;
        }

        private object GetFromHeaders(ParameterWrapper parameter)
        {
            if (!Request.Headers.ContainsKey(parameter.Name)) return null;
            var vals = Request.Headers[parameter.Name];
            if (vals.Count() <= 1) return vals.SingleOrDefault();
            throw new InvalidCastException("Not currently able to deal with collections...");
        }

        private static ConcurrentDictionary<Type, ErrorHandlerAttribute> errorhanderCache = new ConcurrentDictionary<Type, ErrorHandlerAttribute>();
        protected void InitializeServiceApi(Type interfaceType)
        {
            var assemblyThrottler = interfaceType.Assembly.GetCustomAttributes<ThrottlingAttribute>().FirstOrDefault();
            var serviceThrottler = interfaceType.GetCustomAttributes<ThrottlingAttribute>().FirstOrDefault();

            var classInitializers = interfaceType.GetCustomAttributes<ServiceInitializerAttribute>();
            var serviceInitializerAttributes = classInitializers as ServiceInitializerAttribute[] ?? classInitializers.ToArray();
            GetErrorInterceptor(interfaceType);
            ConcurrentDictionary<string, ActionWrapper> wrapper;
            if (cache.TryGetValue(interfaceType, out wrapper)) return;
            var newWrapper = new ConcurrentDictionary<string, ActionWrapper>();

            foreach (var methodInfo in interfaceType.GetMethods().Length == 0 ? interfaceType.GetInterfaces().First().GetMethods() : interfaceType.GetMethods())
            {
                var methodThrottler = methodInfo.GetCustomAttributes<ThrottlingAttribute>().FirstOrDefault();
                var methodInitializers = methodInfo.GetCustomAttributes<ServiceInitializerAttribute>().ToList();
                methodInitializers.AddRange(serviceInitializerAttributes);
                var template = ExtensionsFactory.GetServiceTemplate(methodInfo, _serviceLocator);
                var actionName = GetActionName(methodInfo);
                var action = new ActionWrapper { Name = actionName, ReturnType = methodInfo.ReturnType, RouteTemplate = template, Parameters = new List<ParameterWrapper>() };
                var actions = methodInfo.GetCustomAttributes(true).OfType<VerbAttribute>();
                var methods = ExtensionsFactory.GetHttpMethods(actions.ToList(), methodInfo, _serviceLocator);
                var handlers = ExtensionsFactory.GetHeaderInspectors(methodInfo, _serviceLocator);
                action.CustomHandlers = handlers.ToList();
                action.Actions = methods;

                action.Interceptor = methodInfo.GetCustomAttributes().OfType<InputInterceptorAttribute>().ToArray();
                action.Initializers = methodInitializers;
                if (assemblyThrottler != null)
                    action.Throttler = assemblyThrottler.GetManager(AppliesToTypes.Host);
                else if (serviceThrottler != null)
                    action.Throttler = serviceThrottler.GetManager(AppliesToTypes.Service);
                else if (methodThrottler != null)
                    action.Throttler = methodThrottler.GetManager(AppliesToTypes.Method);
                BuildParameterInfo(methodInfo, action);

                newWrapper.TryAdd(action.Name, action);
            }
            if (cache.TryGetValue(interfaceType, out wrapper)) return;
            cache.TryAdd(interfaceType, newWrapper);
        }

        private void GetErrorInterceptor(Type interfaceType)
        {
            ErrorHandlerAttribute errorHanderInterceptor;
            if (!errorhanderCache.TryGetValue(interfaceType, out errorHanderInterceptor))
            {
                errorHanderInterceptor = interfaceType.GetCustomAttribute<ErrorHandlerAttribute>();
                errorhanderCache.TryAdd(interfaceType, errorHanderInterceptor);
            }
            if (errorHanderInterceptor != null) errorInterceptor = errorHanderInterceptor.ErrorHandler(_serviceLocator);
        }

        private List<IHeaderHandler> GetHeaderInspectors(MethodInfo methodInfo)
        {
            var inspectors = methodInfo.GetCustomAttributes().OfType<IHeaderInspector>();
            var handlers = new List<IHeaderHandler>();
            foreach (var inspector in inspectors)
            {
                handlers.AddRange(inspector.GetHandlers(new Locator(_serviceLocator)));
            }
            return handlers;
        }

        //private static List<HttpMethod> GetHttpMethods(IEnumerable<IActionHttpMethodProvider> actions)
        //{
        //    var methods = new List<HttpMethod>();
        //    foreach (var actionHttpMethodProvider in actions)
        //    {
        //        methods.AddRange(actionHttpMethodProvider.HttpMethods);
        //    }
        //    if (methods.Count == 0) methods.Add(HttpMethod.Get);
        //    return methods;
        //}

        protected string GetActionName(MethodInfo methodInfo)
        {
            var actionName = methodInfo.Name;
            return GetActionName(actionName);
        }

        private static string GetActionName(string actionName)
        {
            if (actionName.EndsWith("Async")) actionName = actionName.Replace("Async", "");
            return actionName;
        }

        private static void BuildParameterInfo(MethodInfo methodInfo, ActionWrapper action)
        {
            foreach (var parameterInfo in methodInfo.GetParameters())
            {
                var @in = parameterInfo.GetCustomAttribute<InAttribute>(true);
                if (@in == null)
                {
                    var fromBody = parameterInfo.GetCustomAttribute<FromBodyAttribute>(true);
                    if (fromBody != null)
                        @in = new InAttribute(InclutionTypes.Body);
                    if (@in == null)
                    {
                        var fromUri = parameterInfo.GetCustomAttribute<FromRouteAttribute>(true) ?? parameterInfo.GetCustomAttribute<FromQueryAttribute>(true) as IBindingSourceMetadata;
                        if (fromUri != null)
                            @in = new InAttribute(InclutionTypes.Path);
                    }
                }
                action.Parameters.Add(new ParameterWrapper { Name = parameterInfo.Name, Type = parameterInfo.ParameterType, In = @in?.InclutionType ?? InclutionTypes.Body });
            }
        }

        protected async Task<IActionResult> ExecuteMethodAsync<TMessage>(Func<Task<TMessage>> func)
        {
            try
            {

                var action = GetAction(_name);
                await ExecuteInterceptorsAsync(action, _wrappers);
                AggregateException error = null;
                var result = await func().ContinueWith(a =>
                {
                    if (a.IsFaulted)
                    {
                        error = a.Exception;
                        return default(TMessage);
                    }
                    else
                    {
                        return a.Result;
                    }
                });
                if (error != null) return CreateErrorResponse(error.InnerException);
                return CreateResponse(HttpStatusCode.OK, result);
            }
            catch (Exception ex)
            {
                return CreateErrorResponse(ex);
            }
        }
        private List<ParameterWrapper> _wrappers;
        private string _name;

        protected async Task<IActionResult> ExecuteMethodVoidAsync(Func<Task> func)
        {
            try
            {
                var action = GetAction(_name);
                await func();
                await ExecuteInterceptorsAsync(action, _wrappers);
                return CreateResponse(HttpStatusCode.NoContent, (object)null);
            }
            catch (Exception ex)
            {
                return CreateErrorResponse(ex);
            }
        }
    }

    internal class ErrorDescriptor
    {
        public string Message { get; set; }
    }
}
