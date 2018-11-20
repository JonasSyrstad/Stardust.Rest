using Newtonsoft.Json;
using Stardust.Interstellar.Rest.Annotations;
using Stardust.Interstellar.Rest.Annotations.Messaging;
using Stardust.Interstellar.Rest.Client.CircuitBreaker;
using Stardust.Interstellar.Rest.Common;
using Stardust.Interstellar.Rest.Extensions;
using Stardust.Interstellar.Rest.Service;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace Stardust.Interstellar.Rest.Client
{
    public class RestWrapper : IServiceWithGlobalParameters
    {


        public void SetHttpHeader(string name, string value)
        {
            additionalHeaders.TryAdd(name, value);
        }

        private ConcurrentDictionary<string, string> additionalHeaders = new ConcurrentDictionary<string, string>();
        private IAuthenticationHandler authenticationHandler;
        private readonly IServiceProvider _serviceLocator;

        private readonly IEnumerable<IHeaderHandler> headerHandlers;

        private readonly Type interfaceType;

        private static ConcurrentDictionary<Type, ConcurrentDictionary<string, ActionWrapper>> cache = new ConcurrentDictionary<Type, ConcurrentDictionary<string, ActionWrapper>>();

        private string baseUri;

        private readonly CookieContainer cookieContainer;

        private ErrorHandlerAttribute _errorInterceptor;
        private readonly ILogger _logger;

        internal static ConcurrentDictionary<Type, ConcurrentDictionary<string, ActionWrapper>> Cache() => cache;

        public void SetBaseUrl(string url)
        {
            baseUri = url;
        }

        public Action<Dictionary<string, object>> Extras { get; internal set; }

        protected RestWrapper(IAuthenticationHandler authenticationHandler, IHeaderHandlerFactory headerHandlers, TypeWrapper interfaceType, IServiceProvider serviceLocator)
        {
            this.authenticationHandler = authenticationHandler;
            _serviceLocator = serviceLocator;
            this.headerHandlers = headerHandlers?.GetHandlers(_serviceLocator) ?? new List<IHeaderHandler>();
            this.interfaceType = interfaceType.Type;
            InitializeClient(this.interfaceType);
            cookieContainer = _serviceLocator.GetService<CookieContainer>() ?? new CookieContainer();
            _logger = serviceLocator.GetService<ILogger>();
        }

        public void InitializeClient(Type interfaceType)
        {
            if (cache.TryGetValue(interfaceType, out ConcurrentDictionary<string, ActionWrapper> wrapper)) return;
            var newWrapper = new ConcurrentDictionary<string, ActionWrapper>();
            var templatePrefix = interfaceType.GetCustomAttribute<IRoutePrefixAttribute>()
                ?? interfaceType.GetInterfaces().FirstOrDefault()?.GetCustomAttribute<IRoutePrefixAttribute>();
            _errorInterceptor = interfaceType.GetCustomAttribute<ErrorHandlerAttribute>();
            _serviceLocator.GetService<ILogger>()?.Message($"Initializing client {interfaceType.Name}");
            var retry = interfaceType.GetCustomAttribute<RetryAttribute>();
            if (interfaceType.GetCustomAttribute<CircuitBreakerAttribute>() != null)
            {
                CircuitBreakerContainer.Register(interfaceType, new CircuitBreaker.CircuitBreaker(interfaceType.GetCustomAttribute<CircuitBreakerAttribute>(), _serviceLocator) { ServiceName = interfaceType.FullName });
            }
            else
            {
                CircuitBreakerContainer.Register(interfaceType, new NullBreaker());
            }
            foreach (var methodInfo in interfaceType.GetMethods().Length == 0 ? interfaceType.GetInterfaces().First().GetMethods() : interfaceType.GetMethods())
            {
                try
                {
                    var actionRetry = methodInfo.GetCustomAttribute<RetryAttribute>() ?? retry;
                    _serviceLocator.GetService<ILogger>()?.Message($"Initializing client action {interfaceType.Name}.{methodInfo.Name}");
                    var template = methodInfo.GetCustomAttribute<IRouteAttribute>();
                    var actionName = GetActionName(methodInfo);
                    var action = new ActionWrapper { Name = actionName, ReturnType = methodInfo.ReturnType, RouteTemplate = ExtensionsFactory.GetRouteTemplate(templatePrefix, template, methodInfo, _serviceLocator), Parameters = new List<ParameterWrapper>() };
                    var actions = methodInfo.GetCustomAttributes(true).OfType<VerbAttribute>().ToList();
                    var methods = ExtensionsFactory.GetHttpMethods(actions, methodInfo, _serviceLocator);
                    var handlers = ExtensionsFactory.GetHeaderInspectors(methodInfo, _serviceLocator);

                    action.UseXml = methodInfo.GetCustomAttributes().OfType<UseXmlAttribute>().Any();
                    action.CustomHandlers = handlers.Where(h => headerHandlers.All(parent => parent.GetType() != h.GetType())).ToList();
                    action.ErrorHandler = _errorInterceptor;//?.ErrorHandler(_serviceLocator);
                    action.Actions = methods;
                    if (actionRetry != null)
                    {
                        action.Retry = true;
                        action.Interval = actionRetry.RetryInterval;
                        action.NumberOfRetries = actionRetry.NumberOfRetries;
                        action.IncrementalRetry = actionRetry.IncremetalWait;
                        if (actionRetry.ErrorCategorizer != null)
                            action.ErrorCategorizer = (IErrorCategorizer)Activator.CreateInstance(actionRetry.ErrorCategorizer, _serviceLocator);
                    }
                    ExtensionsFactory.BuildParameterInfo(methodInfo, action, _serviceLocator);
                    newWrapper.TryAdd(action.Name, action);
                }
                catch (Exception ex)
                {
                    _logger?.Error(ex);
                    throw;
                }
            }
            if (cache.TryGetValue(interfaceType, out wrapper)) return;
            cache.TryAdd(interfaceType, newWrapper);
        }


        protected string GetActionName(MethodInfo methodInfo)
        {
            var actionName = methodInfo.Name;
            return GetActionName(actionName);
        }

        internal static string GetActionName(string actionName)
        {
            if (actionName.EndsWith("Async")) actionName = actionName.Replace("Async", "");
            return actionName;
        }

        public ResultWrapper Execute(string name, ParameterWrapper[] parameters)
        {
            var action = GetAction(name);
            var path = BuildActionUrl(parameters, action);
            var cnt = 0;
            var waittime = action.Interval;
            while (cnt <= action.NumberOfRetries)
            {
                cnt++;
                try
                {
                    var result = CircuitBreakerContainer.GetCircuitBreaker(interfaceType).Execute($"{baseUri}{path}", () => InvokeAction(name, parameters, action, path), _serviceLocator);
                    if (result.Error != null)
                    {
                        if (result.Error is SuspendedDependencyException) return result;
                        if (!action.Retry || cnt > action.NumberOfRetries || !IsTransient(action, result.Error)) return result;
                        _serviceLocator.GetService<ILogger>()?.Error(result.Error);
                        _serviceLocator.GetService<ILogger>()?.Message($"Retrying action {action.Name}, retry count {cnt}");
                        waittime = waittime * (action.IncrementalRetry ? cnt : 1);
                        result.EndState();
                        Thread.Sleep(waittime);
                    }
                    else
                    {
                        if (cnt > 1) result.GetState().Extras.Add("retryCount", cnt);
                        return result;
                    }
                }
                catch (Exception ex)
                {
                    _logger?.Error(ex);
                    if (!action.Retry || cnt > action.NumberOfRetries || !IsTransient(action, ex)) throw;
                    Thread.Sleep(action.IncrementalRetry ? cnt : 1 * action.Interval);
                }
            }
            throw new Exception("Should not get here!?!");

        }

        private ResultWrapper InvokeAction(string name, ParameterWrapper[] parameters, ActionWrapper action, string path)
        {
            var req = CreateActionRequest(parameters, action, path);
            ResultWrapper errorResult;
            HttpWebResponse response = null;
            try
            {
                response = req.GetResponse() as HttpWebResponse;
                EnsureActionId(req, response);
                GetHeaderValues(action, response);
                var result = CreateResult(action, response);
                result.ActionId = req.ActionId();
                return result;
            }
            catch (WebException webError)
            {
                _logger?.Error(webError);
                EnsureActionId(webError, req);

                GetHeaderValues(action, webError.Response as HttpWebResponse);
                errorResult = HandleWebException(webError, action);
                //try
                //{
                //    webError.Response?.Close();
                //    webError.Response?.Dispose();
                //}
                //catch (Exception)
                //{
                //}
            }
            catch (Exception ex)
            {
                _logger?.Error(ex);
                errorResult = HandleGenericException(ex);
            }
            finally
            {
                try
                {
                    response?.Close();
                    response?.Dispose();
                }
                catch (Exception ex)
                {
                    _logger?.Error(ex);
                }
            }
            errorResult.ActionId = req.ActionId();
            return errorResult;
        }

        private void GetHeaderValues(ActionWrapper action, HttpWebResponse response)
        {
            if (response == null) return;
            if (response.Cookies != null && response.Cookies.Count > 0)
                cookieContainer.Add(response.Cookies);
            var handler = new List<IHeaderHandler>();
            foreach (var customHandler in action.CustomHandlers)
            {
                handler.AddRange(customHandler.GetHandlers(_serviceLocator));
            }

            foreach (var customHandler in handler.OrderBy(f => f.ProcessingOrder))
            {
                customHandler.GetHeader(response);
            }
        }

        private static ResultWrapper HandleGenericException(Exception ex)
        {
            return new ResultWrapper { Status = HttpStatusCode.BadGateway, StatusMessage = ex.Message, Error = ex };
        }

        private static ResultWrapper HandleWebException(WebException webError, ActionWrapper action)
        {
            var resp = webError.Response as HttpWebResponse;
            if (resp != null)
            {
                var result = TryGetErrorBody(action, resp);
                return new ResultWrapper
                {
                    Status = resp.StatusCode,

                    StatusMessage = resp.StatusDescription,
                    Error = webError,
                    Value = result
                };
            }
            return new ResultWrapper { Status = HttpStatusCode.BadGateway, StatusMessage = webError.Message, Error = webError };
        }

        private static string TryGetErrorBody(ActionWrapper action, HttpWebResponse resp)
        {
            try
            {
                using (var reader = new StreamReader(resp.GetResponseStream()))
                {
                    return reader.ReadToEnd();
                }
            }
            catch
            {
                // ignored
            }
            return null;
        }

        private ResultWrapper CreateResult(ActionWrapper action, HttpWebResponse response)
        {
            var type = typeof(Task).IsAssignableFrom(action.ReturnType) ? action.ReturnType.GetGenericArguments().FirstOrDefault() : action.ReturnType;
            if (type == typeof(void) || type == null)
            {
                return new ResultWrapper { Type = typeof(void), IsVoid = true, Value = null, Status = response.StatusCode, StatusMessage = response.StatusDescription, ActionId = response.ActionId() };
            }
            var result = GetResultFromResponse(action, response, type);
            return new ResultWrapper { Type = type, IsVoid = false, Value = result, Status = response.StatusCode, StatusMessage = response.StatusDescription, ActionId = response.ActionId() };
        }

        private object GetResultFromResponse(ActionWrapper action, HttpWebResponse response, Type type)
        {
            object result;
            if (action.UseXml)
            {
                result = GetSerializer().Deserialize(response.GetResponseStream(), type);
            }
            else
            {
                using (var reader = new JsonTextReader(new StreamReader(response.GetResponseStream())))
                {
                    var serializer = CreateJsonSerializer(type);
                    result = serializer.Deserialize(reader, type);
                }
            }
            return result;
        }

        private JsonSerializer CreateJsonSerializer(Type getType)
        {
            JsonSerializerSettings serializerSettings = null;
            if (getType != null) serializerSettings = getType.GetClientSerializationSettings();
            if (serializerSettings == null)
            {
                serializerSettings = interfaceType.GetClientSerializationSettings();
            }
            var serializer = serializerSettings == null ? JsonSerializer.Create() : JsonSerializer.Create(serializerSettings);
            return serializer;
        }

        private HttpWebRequest CreateActionRequest(ParameterWrapper[] parameters, ActionWrapper action, string path)
        {
            var req = CreateActionRequestBase(parameters, action, path);
            if (authenticationHandler == null) authenticationHandler = _serviceLocator.GetService<IAuthenticationHandler>();
            if (ProxyFactory.RunAuthProviderBeforeAppendingBody)
                authenticationHandler?.Apply(req);
            AppendBody(parameters, req, action);
            if (!ProxyFactory.RunAuthProviderBeforeAppendingBody)
                authenticationHandler?.Apply(req);
            return req;
        }

        private async Task<HttpWebRequest> CreateActionRequestAsync(ParameterWrapper[] parameters, ActionWrapper action, string path)
        {
            var req = CreateActionRequestBase(parameters, action, path);
            if (authenticationHandler == null) authenticationHandler = _serviceLocator.GetService<IAuthenticationHandler>();
            if (ProxyFactory.RunAuthProviderBeforeAppendingBody)
                if (authenticationHandler != null) await authenticationHandler.ApplyAsync(req);
            req = await AppendBodyAsync(parameters, req, action);
            if (!ProxyFactory.RunAuthProviderBeforeAppendingBody)
                if (authenticationHandler != null) await authenticationHandler.ApplyAsync(req);
            return req;
        }

        private HttpWebRequest CreateActionRequestBase(ParameterWrapper[] parameters, ActionWrapper action, string path)
        {
            var req = action.UseXml ? CreateRequest(path, "application/xml") : CreateRequest(path);

            if (DisableProxy) req.Proxy = null;
            req.Headers.Add(ExtensionsFactory.ActionIdName, Guid.NewGuid().ToString());
            req.InitializeState();
            req.Method = action.Actions.First().ToString();
            req.GetState().Extras.Add("serviceRoot", baseUri);
            AppendHeaders(parameters, req, action);

            return req;
        }

        private static string BuildActionUrl(ParameterWrapper[] parameters, ActionWrapper action)
        {
            if (action == null) Console.WriteLine($"no action definition found ?!?!");
            var path = action?.RouteTemplate ?? "";
            var queryStrings = new List<string>();
            if (parameters == null) return path;
            foreach (var source in parameters.Where(p => p.In == InclutionTypes.Path))
            {
                if (path.Contains($"{{{source.Name}}}"))
                {
                    path = path.Replace($"{{{source.Name}}}", Uri.EscapeDataString(source?.value?.ToString() ?? ""));
                }
                else
                {
                    queryStrings.Add($"{source.Name}={HttpUtility.UrlEncode(source?.value?.ToString() ?? "")}");
                }
            }
            if (queryStrings.Any()) path = path + "?" + string.Join("&", queryStrings);
            return path;
        }

        public static bool DisableProxy { get; set; }

        private ActionWrapper GetAction(string name)
        {
            if (!cache.TryGetValue(interfaceType, out ConcurrentDictionary<string, ActionWrapper> @interface)) throw new InvalidOperationException("Unknown interface type");

            if (!@interface.TryGetValue(GetActionName(name), out ActionWrapper action)) throw new InvalidOperationException("Unknown method");
            return action;
        }

        private static string Version { get; } = $"{typeof(GetAttribute).Assembly.GetName().Version.Major}.{typeof(GetAttribute).Assembly.GetName().Version.Minor}";

        private HttpWebRequest CreateRequest(string path, string contentType = "application/json")
        {
            var req = WebRequest.Create(new Uri($"{baseUri}/{path}")) as HttpWebRequest;
            req.AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip;
            req.Accept = contentType;
            req.ContentType = contentType;
            req.Headers.Add("Accept-Language", "en-us");
            req.UserAgent = $"stardust/{Version}";
            req.AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip;
            req.CookieContainer = cookieContainer;
            SetExtraHeaderValues(req);
            SetTimeoutValues(req);
            return req;
        }

        private void SetExtraHeaderValues(HttpWebRequest req)
        {
            foreach (var additionalHeader in additionalHeaders)
            {
                try
                {
                    req.Headers.Add(additionalHeader.Key, additionalHeader.Value);
                }
                catch (Exception ex)
                {
                    _serviceLocator.GetService<ILogger>()?.Error(ex);
                }
            }
        }

        private static void SetTimeoutValues(HttpWebRequest req)
        {
            if (ClientGlobalSettings.Timeout != null) req.Timeout = ClientGlobalSettings.Timeout.Value;
            if (ClientGlobalSettings.ReadWriteTimeout != null) req.ReadWriteTimeout = ClientGlobalSettings.ReadWriteTimeout.Value;
            if (ClientGlobalSettings.ContinueTimeout != null) req.ContinueTimeout = ClientGlobalSettings.ContinueTimeout.Value;
        }

        private void AppendBody(ParameterWrapper[] parameters, HttpWebRequest req, ActionWrapper action)
        {
            if (parameters.Any(p => p.In == InclutionTypes.Body))
            {
                if (parameters.Count(p => p.In == InclutionTypes.Body) > 1)
                {
                    SerializeBody(req, parameters.Where(p => p.In == InclutionTypes.Body).Select(p => p.value).ToList(), action);
                }
                else
                {
                    var val = parameters.Single(p => p.In == InclutionTypes.Body);
                    SerializeBody(req, val.value, action);
                }
            }
            else
            {
                req.ContentLength = 0;
            }
            if (req.ContentType.Contains("xml")) req.ContentType = null;
        }

        private async Task<HttpWebRequest> AppendBodyAsync(ParameterWrapper[] parameters, HttpWebRequest req, ActionWrapper action)
        {
            if (parameters.Any(p => p.In == InclutionTypes.Body))
            {
                if (parameters.Count(p => p.In == InclutionTypes.Body) > 1)
                {
                    return await SerializeBodyAsync(req, parameters.Where(p => p.In == InclutionTypes.Body).Select(p => p.value).ToList(), action);
                }
                else
                {
                    var val = parameters.Single(p => p.In == InclutionTypes.Body);
                    return await SerializeBodyAsync(req, val.value, action);
                }
            }
            else
            {
                req.ContentLength = 0;
            }
            if (req.ContentType.Contains("xml")) req.ContentType = null;

            return req;
        }

        private void AppendHeaders(ParameterWrapper[] parameters, HttpWebRequest req, ActionWrapper action)
        {
            foreach (var source in parameters.Where(p => p.In == InclutionTypes.Header))
            {
                req.Headers.Add(string.Format("{0}", source.Name), source.value?.ToString());
            }
            if (action.CustomHandlers != null)
            {
                var handler = new List<IHeaderHandler>();
                foreach (var customHandler in action.CustomHandlers)
                {
                    handler.AddRange(customHandler.GetHandlers(_serviceLocator));
                }

                foreach (var customHandler in handler.OrderBy(f => f.ProcessingOrder))
                {
                    customHandler.SetHeader(req);
                }
            }
            if (headerHandlers == null) return;
            foreach (var headerHandler in headerHandlers)
            {
                headerHandler.SetHeader(req);
            }


        }

        private void SerializeBody(WebRequest req, object val, ActionWrapper action)
        {
            if (action.UseXml) XmlBodySerializer(req, val);
            else
            {
                if (typeof(IServiceWithGlobalParameters).IsAssignableFrom(interfaceType))
                    JsonBodySerializer(req, GlobalParameterExtensions.AppendGlobalParameters(interfaceType.FullName, val, action.MessageExtesionLevel));
                else
                    JsonBodySerializer(req, val);
            }
        }

        private async Task<HttpWebRequest> SerializeBodyAsync(HttpWebRequest req, object val, ActionWrapper action)
        {
            if (val == null)
            {
                req.ContentLength = 0;
                return req;
            }
            if (action.UseXml)
            {
                XmlBodySerializer(req, val);
                return req;
            }
            else
            {
                if (typeof(IServiceWithGlobalParameters).IsAssignableFrom(interfaceType))
                    return await JsonBodySerializerAsync(req, GlobalParameterExtensions.AppendGlobalParameters(interfaceType.FullName, val, action.MessageExtesionLevel));
                else
                    return await JsonBodySerializerAsync(req, val);
            }
        }

        private void XmlBodySerializer(WebRequest req, object val)
        {
            var xmlSerializer = GetSerializer();
            xmlSerializer.Serialize(req, val);
        }

        private ISerializer GetSerializer()
        {
            var xmlSerializer = new Locator(_serviceLocator).GetServices<ISerializer>().SingleOrDefault(s => string.Equals(s.SerializationType, "xml", StringComparison.InvariantCultureIgnoreCase));
            if (xmlSerializer == null) throw new IndexOutOfRangeException("Could not find serializer");
            return xmlSerializer;
        }

        private void JsonBodySerializer(WebRequest req, object val)
        {

            var serializer = CreateJsonSerializer(val?.GetType());
            byte[] buffer;
            using (var memStream = new MemoryStream())
            {
                using (var stream = new StreamWriter(memStream))
                {
                    using (var writer = new JsonTextWriter(stream))
                    {
                        serializer.Serialize(writer, val);
                    }
                }
                buffer = memStream.ToArray();
            }
            req.ContentLength = buffer.Length;
            var requestStream = req.GetRequestStream();
            requestStream.Write(buffer, 0, buffer.Length);
        }

        private async Task<HttpWebRequest> JsonBodySerializerAsync(HttpWebRequest req, object val)
        {

            var serializer = CreateJsonSerializer(val?.GetType());
            byte[] buffer;
            using (var memStream = new MemoryStream())
            {
                using (var stream = new StreamWriter(memStream))
                {
                    using (var writer = new JsonTextWriter(stream))
                    {
                        serializer.Serialize(writer, val);
                    }
                }
                buffer = memStream.ToArray();
            }
            req.ContentLength = buffer.Length;
            var requestStream = await req.GetRequestStreamAsync();
            await requestStream.WriteAsync(buffer, 0, buffer.Length);
            return req;
        }

        public async Task<ResultWrapper> ExecuteAsync(string name, ParameterWrapper[] parameters)
        {
            var action = GetAction(name);
            var path = BuildActionUrl(parameters, action);
            int cnt = 0;
            var waittime = action.Interval;
            while (cnt <= action.NumberOfRetries)
            {
                cnt++;
                try
                {
                    var result = await CircuitBreakerContainer.GetCircuitBreaker(interfaceType).ExecuteAsync($"{baseUri}{path}", async () => await InvokeActionAsync(parameters, action, path), _serviceLocator);
                    if (result.Error != null)
                    {
                        if (result.Error is SuspendedDependencyException)
                            return result;
                        if (!action.Retry || cnt > action.NumberOfRetries || !IsTransient(action, result.Error)) return result;
                        _serviceLocator.GetService<ILogger>()?.Error(result.Error);
                        _serviceLocator.GetService<ILogger>()?.Message($"Retrying action {action.Name}, retry count {cnt}");
                        waittime = waittime * (action.IncrementalRetry ? cnt : 1);
                        result.EndState();
                        await Task.Delay(waittime);
                    }
                    else
                    {
                        if (cnt > 1) result.GetState().Extras.Add("retryCount", cnt);
                        return result;
                    }
                }
                catch (Exception ex)
                {

                    if (!action.Retry || cnt > action.NumberOfRetries || !IsTransient(action, ex)) throw;
                    await Task.Delay(action.IncrementalRetry ? cnt : 1 * action.Interval);
                }
            }
            throw new Exception("Should not get here!?!");
        }

        private bool IsTransient(ActionWrapper action, Exception exception)
        {
            if (action.ErrorCategorizer != null)
            {
                return action.ErrorCategorizer.IsTransientError(exception);
            }
            var webException = exception as WebException;
            if (webException != null)
            {
                return new[] {WebExceptionStatus.ConnectionClosed,
                  WebExceptionStatus.Timeout,
                  WebExceptionStatus.RequestCanceled ,WebExceptionStatus.ConnectFailure}.
                        Contains(webException.Status);
            }

            return false;

        }

        private async Task<ResultWrapper> InvokeActionAsync(ParameterWrapper[] parameters, ActionWrapper action, string path)
        {
            var req = await CreateActionRequestAsync(parameters, action, path);
            ResultWrapper errorResult;
            HttpWebResponse response = null;
            try
            {
                response = await req.GetResponseAsync() as HttpWebResponse;
                EnsureActionId(req, response);
                GetHeaderValues(action, response);
                var result = CreateResult(action, response);
                result.ActionId = req.ActionId();
                return result;
            }
            catch (WebException webError)
            {
                EnsureActionId(webError, req);
                GetHeaderValues(action, webError.Response as HttpWebResponse);
                errorResult = HandleWebException(webError, action);
                //tryr
                //{
                //    webError.Response?.Close();
                //    webError.Response?.Dispose();
                //}
                //catch (Exception)
                //{
                //}
            }
            catch (Exception ex)
            {
                errorResult = HandleGenericException(ex);
            }
            finally
            {
                try
                {
                    response?.Close();
                    response?.Dispose();
                }
                catch (Exception)
                {
                }
            }
            errorResult.ActionId = req.ActionId();
            return errorResult;
        }

        private static void EnsureActionId(WebException webError, HttpWebRequest req)
        {
            var resp = webError.Response;
            EnsureActionId(req, resp);
        }

        private static void EnsureActionId(HttpWebRequest req, WebResponse resp)
        {
            if (string.IsNullOrWhiteSpace(resp?.Headers?.Get("sd-ActionId")))
            {
                resp?.Headers?.Add("sd-ActionId", req.ActionId());
            }
        }

        public T Invoke<T>(string name, ParameterWrapper[] parameters)
        {
            var result = Execute(name, parameters);
            Extras?.Invoke(result.GetState().Extras);
            result.EndState();

            if (result.Error == null)
                return (T)result.Value;
            CreateException(name, result);
            return default(T);
        }

        public async Task<T> InvokeAsync<T>(string name, ParameterWrapper[] parameters)
        {
            var result = await ExecuteAsync(GetActionName(name), parameters);
            Extras?.Invoke(result.GetState().Extras);
            StateHelper.EndState(result.ActionId);
            if (typeof(T) == typeof(void)) return default(T);
            if (result.Error == null)
                return (T)result.Value;
            CreateException(name, result);
            return default(T);
        }

        private void CreateException(string name, ResultWrapper result)
        {
            var action = GetAction(name);
            if (result?.Status == (HttpStatusCode)429)
                throw new RestWrapperException(result?.StatusMessage, (HttpStatusCode)429, new ThrottledRequestException(result?.Error));
            var handler = GetErrorHandler(action);
            if (handler != null) throw handler.ProduceClientException(result.StatusMessage, result.Status, result.Error, result.Value as string);
            if (result?.Value != null) throw new RestWrapperException(result.StatusMessage, result.Status, result.Value, result.Error);
            throw new RestWrapperException(result?.StatusMessage, result?.Status ?? HttpStatusCode.Unused, result?.Error);
        }

        private IErrorHandler GetErrorHandler(ActionWrapper action)
        {
            IErrorHandler handler;
            var globalErrorHandler = _serviceLocator.GetService<IErrorHandler>();
            var errorHandler = action?.ErrorHandler;
            if (globalErrorHandler != null && errorHandler != null)
            {
                handler = new AggregateHandler(globalErrorHandler, errorHandler?.ErrorHandler(_serviceLocator));
            }
            else if (globalErrorHandler != null)
            {
                handler = globalErrorHandler;
            }
            else
            {
                handler = errorHandler?.ErrorHandler(_serviceLocator);
            }
            return handler;
        }

        public async Task InvokeVoidAsync(string name, ParameterWrapper[] parameters)
        {
            var result = await ExecuteAsync(GetActionName(name), parameters);
            StateHelper.EndState(result.ActionId);
            if (result.Error == null)
                return;
            CreateException(name, result);
        }

        public void InvokeVoid(string name, ParameterWrapper[] parameters)
        {
            var result = Execute(name, parameters);
            StateHelper.EndState(result.ActionId);
            if (result.Error == null)
                return;
            CreateException(name, result);
        }

        protected ParameterWrapper[] GetParameters(string name, params object[] parameters)
        {
            if (!cache.TryGetValue(interfaceType, out ConcurrentDictionary<string, ActionWrapper> item)) throw new InvalidOperationException("Invalid interface type");
            if (!item.TryGetValue(GetActionName(name), out ActionWrapper action)) throw new InvalidOperationException("Invalid action");
            var i = 0;
            var wrappers = new List<ParameterWrapper>();
            foreach (var parameter in parameters)
            {
                var def = action.Parameters[i];
                wrappers.Add(def.Create(parameter));
                i++;
            }
            return wrappers.ToArray();
        }

        public IServiceProvider ServiceLocator => _serviceLocator;
    }
}
