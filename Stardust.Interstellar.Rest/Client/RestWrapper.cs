using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Newtonsoft.Json;
using Stardust.Interstellar.Rest.Annotations;
using Stardust.Interstellar.Rest.Annotations.Messaging;
using Stardust.Interstellar.Rest.Annotations.Rest.Extensions;
using Stardust.Interstellar.Rest.Client.CircuitBreaker;
using Stardust.Interstellar.Rest.Common;
using Stardust.Interstellar.Rest.Extensions;
using Stardust.Interstellar.Rest.Service;
using Stardust.Particles;
using Stardust.Particles.Collection.Arrays;

namespace Stardust.Interstellar.Rest.Client
{
    public class RestWrapper : IServiceWithGlobalParameters, IConfigurableService
    {
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
        private string _trailingQueryString;
        private bool _pathVersionSet;
        private bool? _runAuthProviderBeforeAppendingBody;
        private Func<string, IWebProxy> _proxyFunc;
        private static readonly ConcurrentDictionary<string, HttpClient> _clients = new ConcurrentDictionary<string, HttpClient>();

        public void SetHttpHeader(string name, string value) => additionalHeaders.TryAdd(name, value);

        internal static ConcurrentDictionary<Type, ConcurrentDictionary<string, ActionWrapper>> Cache() => cache;

        public void SetBaseUrl(string url) => baseUri = url;

        public Action<Dictionary<string, object>> Extras { get; internal set; }

        protected RestWrapper(
          IAuthenticationHandler authenticationHandler,
          IHeaderHandlerFactory headerHandlers,
          TypeWrapper interfaceType,
          IServiceProvider serviceLocator)
        {
            this.authenticationHandler = authenticationHandler;
            _serviceLocator = serviceLocator;
            this.headerHandlers = headerHandlers?.GetHandlers(_serviceLocator) ?? new List<IHeaderHandler>();
            this.interfaceType = interfaceType.Type;
            InitializeClient(this.interfaceType);
            cookieContainer = _serviceLocator.GetService<CookieContainer>() ?? new CookieContainer();
            _logger = serviceLocator.GetService<ILogger>();
        }

        private static IRoutePrefix GetRoutePrefix(Type interfaceType)
        {
            ApiAttribute apiAttribute = interfaceType.GetCustomAttribute<ApiAttribute>();
            if (apiAttribute == null)
            {
                Type element = interfaceType.GetInterfaces().FirstOrDefault();
                apiAttribute = (object)element != null ? element.GetCustomAttribute<ApiAttribute>() : null;
            }
            IRoutePrefix routePrefix = apiAttribute;
            if (routePrefix == null)
            {
                IRoutePrefixAttribute iroutePrefixAttribute = interfaceType.GetCustomAttribute<IRoutePrefixAttribute>();
                if (iroutePrefixAttribute == null)
                {
                    Type element = interfaceType.GetInterfaces().FirstOrDefault();
                    iroutePrefixAttribute = (object)element != null ? element.GetCustomAttribute<IRoutePrefixAttribute>() : null;
                }
                routePrefix = iroutePrefixAttribute;
            }
            return routePrefix;
        }

        public void InitializeClient(Type interfaceType)
        {
            ConcurrentDictionary<string, ActionWrapper> concurrentDictionary1;
            if (cache.TryGetValue(interfaceType, out concurrentDictionary1))
                return;
            ConcurrentDictionary<string, ActionWrapper> concurrentDictionary2 = new ConcurrentDictionary<string, ActionWrapper>();
            IRoutePrefix routePrefix = GetRoutePrefix(interfaceType);
            _errorInterceptor = interfaceType.GetCustomAttribute<ErrorHandlerAttribute>();
            _serviceLocator.GetService<ILogger>()?.Message("Initializing client " + interfaceType.Name);
            RetryAttribute customAttribute = interfaceType.GetCustomAttribute<RetryAttribute>();
            if (interfaceType.GetCustomAttribute<CircuitBreakerAttribute>() != null)
                CircuitBreakerContainer.Register(interfaceType, new CircuitBreaker.CircuitBreaker(interfaceType.GetCustomAttribute<CircuitBreakerAttribute>(), _serviceLocator)
                {
                    ServiceName = interfaceType.FullName
                });
            else
                CircuitBreakerContainer.Register(interfaceType, new NullBreaker());
            foreach (MethodInfo methodInfo in interfaceType.GetMethods().Length == 0 ? interfaceType.GetInterfaces().First().GetMethods() : interfaceType.GetMethods())
            {
                try
                {
                    RetryAttribute retryAttribute = methodInfo.GetCustomAttribute<RetryAttribute>() ?? customAttribute;
                    _serviceLocator.GetService<ILogger>()?.Message("Initializing client action " + interfaceType.Name + "." + methodInfo.Name);
                    IRoute template = (IRoute)methodInfo.GetCustomAttribute<IRouteAttribute>() ?? methodInfo.GetCustomAttribute<VerbAttribute>();
                    string actionName = GetActionName(methodInfo);
                    List<VerbAttribute> list = methodInfo.GetCustomAttributes(true).OfType<VerbAttribute>().ToList();
                    ActionWrapper action = new ActionWrapper
                    {
                        Name = actionName,
                        ReturnType = methodInfo.ReturnType,
                        RouteTemplate = ExtensionsFactory.GetRouteTemplate(routePrefix, template, methodInfo, _serviceLocator),
                        Parameters = new List<ParameterWrapper>()
                    };
                    MethodInfo method = methodInfo;
                    IServiceProvider serviceLocator = _serviceLocator;
                    List<HttpMethod> httpMethods = ExtensionsFactory.GetHttpMethods(list, method, serviceLocator);
                    List<IHeaderInspector> headerInspectors = ExtensionsFactory.GetHeaderInspectors(methodInfo, _serviceLocator);
                    action.UseXml = methodInfo.GetCustomAttributes().OfType<UseXmlAttribute>().Any();
                    action.CustomHandlers = headerInspectors.Where(h => headerHandlers.All(parent => parent.GetType() != h.GetType())).ToList();
                    action.ErrorHandler = _errorInterceptor;
                    action.Actions = httpMethods;
                    if (retryAttribute != null)
                    {
                        action.Retry = true;
                        action.Interval = retryAttribute.RetryInterval;
                        action.NumberOfRetries = retryAttribute.NumberOfRetries;
                        action.IncrementalRetry = retryAttribute.IncremetalWait;
                        if (retryAttribute.ErrorCategorizer != null)
                            action.ErrorCategorizer = (IErrorCategorizer)Activator.CreateInstance(retryAttribute.ErrorCategorizer, _serviceLocator);
                    }
                    ExtensionsFactory.BuildParameterInfo(methodInfo, action, _serviceLocator);
                    concurrentDictionary2.TryAdd(action.Name, action);
                }
                catch (Exception ex)
                {
                    _logger?.Error(ex);
                    throw;
                }
            }
            if (cache.TryGetValue(interfaceType, out concurrentDictionary1))
                return;
            cache.TryAdd(interfaceType, concurrentDictionary2);
        }

        protected string GetActionName(MethodInfo methodInfo) => GetActionName(methodInfo.Name);

        internal static string GetActionName(string actionName)
        {
            if (actionName.EndsWith("Async"))
                actionName = actionName.Replace("Async", "");
            return actionName;
        }

        public ResultWrapper Execute(string name, ParameterWrapper[] parameters)
        {
            ActionWrapper action = GetAction(name);
            string path = BuildActionUrl(parameters, action);
            int num = 0;
            int interval = action.Interval;
            while (num <= action.NumberOfRetries)
            {
                ++num;
                try
                {
                    ResultWrapper result = CircuitBreakerContainer.GetCircuitBreaker(interfaceType).Execute(baseUri + path, () => InvokeAction(name, parameters, action, path), _serviceLocator);
                    if (result.Error != null)
                    {
                        if (result.Error is SuspendedDependencyException || !action.Retry || num > action.NumberOfRetries || !IsTransient(action, result.Error))
                            return result;
                        _serviceLocator.GetService<ILogger>()?.Error(result.Error);
                        _serviceLocator.GetService<ILogger>()?.Message(string.Format("Retrying action {0}, retry count {1}", action.Name, num));
                        interval *= action.IncrementalRetry ? num : 1;
                        result.EndState();
                        Thread.Sleep(interval);
                    }
                    else
                    {
                        if (num > 1)
                            result.GetState().Extras.Add("retryCount", num);
                        return result;
                    }
                }
                catch (Exception ex)
                {
                    _logger?.Error(ex);
                    if (!action.Retry || num > action.NumberOfRetries || !IsTransient(action, ex))
                        throw;
                    Thread.Sleep(action.IncrementalRetry ? num : action.Interval);
                }
            }
            throw new Exception("Should not get here!?!");
        }

        private HttpClient GetClient()
        {
            HttpClient httpClient1;
            if (_clients.TryGetValue(baseUri.ToLower(), out httpClient1))
                return httpClient1;
            HttpClient httpClient2 = new HttpClient
            {
                BaseAddress = new Uri(baseUri),
                Timeout = TimeSpan.FromSeconds(ClientGlobalSettings.Timeout ?? 100)
            };
            _clients.TryAdd(baseUri.ToLower(), httpClient2);
            return httpClient2;
        }

        private ResultWrapper InvokeAction(
          string name,
          ParameterWrapper[] parameters,
          ActionWrapper action,
          string path)
        {
            return Task.Run(async () => await InvokeActionAsync(parameters, action, path)).Result;
        }

        private void GetHeaderValues(ActionWrapper action, HttpResponseMessage response)
        {
            if (response == null)
                return;
            List<IHeaderHandler> source = new List<IHeaderHandler>();
            foreach (IHeaderInspector customHandler in action.CustomHandlers)
                source.AddRange(customHandler.GetHandlers(_serviceLocator));
            foreach (IHeaderHandler headerHandler in source.OrderBy(f => f.ProcessingOrder))
                headerHandler.GetHeader(response);
        }

        private static ResultWrapper HandleGenericException(Exception ex) => new ResultWrapper
        {
            Status = HttpStatusCode.BadGateway,
            StatusMessage = ex.Message,
            Error = ex
        };

        private static async Task<ResultWrapper> HandleWebException(
          HttpRequestException webError,
          HttpResponseMessage response,
          ActionWrapper action)
        {
            if (response != null)
            {
                string errorBody = await TryGetErrorBody(action, response);
                return new ResultWrapper
                {
                    Status = response.StatusCode,
                    StatusMessage = response.ReasonPhrase,
                    Error = webError,
                    Value = errorBody
                };
            }
            return new ResultWrapper
            {
                Status = HttpStatusCode.BadGateway,
                StatusMessage = webError.Message,
                Error = new WebException(webError.Message)
            };
        }

        private static async Task<string> TryGetErrorBody(
          ActionWrapper action,
          HttpResponseMessage resp)
        {
            try
            {
                return await resp.Content.ReadAsStringAsync();
            }
            catch
            {
            }
            return null;
        }

        private async Task<ResultWrapper> CreateResult(
          ActionWrapper action,
          HttpResponseMessage response)
        {
            Type type = typeof(Task).IsAssignableFrom(action.ReturnType) ? action.ReturnType.GetGenericArguments().FirstOrDefault() : action.ReturnType;
            if (type == typeof(void) || type == null)
                return new ResultWrapper
                {
                    Type = typeof(void),
                    IsVoid = true,
                    Value = null,
                    Status = response.StatusCode,
                    StatusMessage = response.ReasonPhrase,
                    ActionId = response.ActionId()
                };
            object resultFromResponse = await GetResultFromResponse(action, response, type);
            return new ResultWrapper
            {
                Type = type,
                IsVoid = false,
                Value = resultFromResponse,
                Status = response.StatusCode,
                StatusMessage = response.ReasonPhrase,
                ActionId = response.ActionId()
            };
        }

        private async Task<object> GetResultFromResponse(
          ActionWrapper action,
          HttpResponseMessage response,
          Type type)
        {
            object obj;
            if (action.UseXml)
            {
                ISerializer serializer = GetSerializer();
                obj = serializer.Deserialize(await response.Content.ReadAsStreamAsync(), type);
                serializer = null;
            }
            else
            {
                using (JsonTextReader jsonTextReader = new JsonTextReader(new StreamReader(await response.Content.ReadAsStreamAsync())))
                    obj = CreateJsonSerializer(type).Deserialize(jsonTextReader, type);
            }
            return obj;
        }

        private JsonSerializer CreateJsonSerializer(Type getType)
        {
            JsonSerializerSettings settings = null;
            if (getType != null)
                settings = getType.GetClientSerializationSettings();
            if (settings == null)
                settings = interfaceType.GetClientSerializationSettings();
            return settings != null ? JsonSerializer.Create(settings) : JsonSerializer.Create();
        }

        private HttpRequestMessage CreateActionRequest(
          ParameterWrapper[] parameters,
          ActionWrapper action,
          string path)
        {
            HttpRequestMessage actionRequestBase = CreateActionRequestBase(parameters, action, path);
            byte[] body = PrepareBody(parameters, actionRequestBase, action);
            if (authenticationHandler == null)
                authenticationHandler = _serviceLocator.GetService<IAuthenticationHandler>();
            authenticationHandler?.BodyData(body);
            if (RunAuthProviderBeforeAppendingBody)
                authenticationHandler?.Apply(actionRequestBase);
            if (!RunAuthProviderBeforeAppendingBody)
                authenticationHandler?.Apply(actionRequestBase);
            return actionRequestBase;
        }

        public bool RunAuthProviderBeforeAppendingBody
        {
            get => _runAuthProviderBeforeAppendingBody ?? ProxyFactory.RunAuthProviderBeforeAppendingBody;
            set => _runAuthProviderBeforeAppendingBody = value;
        }

        private async Task<HttpRequestMessage> CreateActionRequestAsync(
          ParameterWrapper[] parameters,
          ActionWrapper action,
          string path)
        {
            HttpRequestMessage req = CreateActionRequestBase(parameters, action, path);
            byte[] body = PrepareBody(parameters, req, action);
            if (authenticationHandler == null)
                authenticationHandler = _serviceLocator.GetService<IAuthenticationHandler>();
            authenticationHandler?.BodyData(body);
            if (RunAuthProviderBeforeAppendingBody && authenticationHandler != null)
                await authenticationHandler.ApplyAsync(req);
            if (req.Method != HttpMethod.Get && req.Method != HttpMethod.Head && req.Method != HttpMethod.Options && req.Method != HttpMethod.Trace)
            {
                req.Content = new ByteArrayContent(body ?? new byte[0]);
                req.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            }
            if (!RunAuthProviderBeforeAppendingBody && authenticationHandler != null)
                await authenticationHandler.ApplyAsync(req);
            HttpRequestMessage httpRequestMessage = req;
            req = null;
            body = null;
            return httpRequestMessage;
        }

        private byte[] PrepareBody(
          ParameterWrapper[] parameters,
          HttpRequestMessage req,
          ActionWrapper action)
        {
            if (!parameters.Any(p => p.In == InclutionTypes.Body))
                return null;
            if (parameters.Count(p => p.In == InclutionTypes.Body) > 1)
                return Serialize(parameters.Where(p => p.In == InclutionTypes.Body).Select(p => p.value).ToList(), action);
            ParameterWrapper parameterWrapper = parameters.Single(p => p.In == InclutionTypes.Body);
            object base64String = parameterWrapper.value;
            if (parameterWrapper.value != null && parameterWrapper.value?.GetType() == typeof(byte[]))
                base64String = Convert.ToBase64String((byte[])parameterWrapper.value);
            return Serialize(base64String, action);
        }

        private byte[] Serialize(object value, ActionWrapper action)
        {
            if (action.UseXml)
                return GetSerializer().Serialize(value);
            using (MemoryStream memoryStream = new MemoryStream())
            {
                using (StreamWriter streamWriter = new StreamWriter(memoryStream))
                {
                    using (JsonTextWriter jsonTextWriter = new JsonTextWriter(streamWriter))
                        CreateJsonSerializer(value?.GetType()).Serialize(jsonTextWriter, value);
                }
                return memoryStream.ToArray();
            }
        }

        private HttpRequestMessage CreateActionRequestBase(
          ParameterWrapper[] parameters,
          ActionWrapper action,
          string path)
        {
            string action1 = action.Actions.First().ToString();
            HttpRequestMessage httpRequestMessage = action.UseXml ? CreateRequest(path, action1, "application/xml") : CreateRequest(path, action1);
            httpRequestMessage.Headers.Add("sd-ActionId", Guid.NewGuid().ToString());
            httpRequestMessage.InitializeState();
            httpRequestMessage.GetState().Extras.Add("serviceRoot", baseUri);
            AppendHeaders(parameters, httpRequestMessage, action);
            return httpRequestMessage;
        }

        private string BuildActionUrl(ParameterWrapper[] parameters, ActionWrapper action)
        {
            if (action == null)
                Console.WriteLine("no action definition found ?!?!");
            string str = action?.RouteTemplate ?? "";
            List<string> source = new List<string>();
            if (parameters == null)
                return str;
            foreach (ParameterWrapper parameterWrapper in parameters.Where(p => p.In == InclutionTypes.Path || p.In == InclutionTypes.Query))
            {
                if(VerboseLogging)
                    Logging.DebugMessage($"Building parameter: {parameterWrapper.Name} with type {parameterWrapper.Type.Name}");
                if (str.Contains("{" + parameterWrapper.Name + "}") && parameterWrapper.In == InclutionTypes.Path)
                    str = str.Replace("{" + parameterWrapper.Name + "}", Uri.EscapeDataString(parameterWrapper?.value?.ToString() ?? ""));
                else
                {
                    if (parameterWrapper.value is Dictionary<string, string> || parameterWrapper.Type==typeof(Dictionary<string,string>))
                        source.Add(string.Join("&", ((Dictionary<string, string>)parameterWrapper.value).Select(v => $"{v.Key}={v.Value}")));
                    else
                        source.Add(parameterWrapper.Name + "=" +
                                   HttpUtility.UrlEncode(parameterWrapper?.value?.ToString() ?? ""));
                }
            }
            if (source.Any())
                str = str + "?" + string.Join("&", source);
            if (str.StartsWith("/"))
                str = str.Substring(1);
            return str;
        }

        public bool VerboseLogging { get; set; }

        public static bool DisableProxy { get; set; }

        private ActionWrapper GetAction(string name)
        {
            ConcurrentDictionary<string, ActionWrapper> concurrentDictionary;
            if (!cache.TryGetValue(interfaceType, out concurrentDictionary))
                throw new InvalidOperationException("Unknown interface type");
            ActionWrapper actionWrapper;
            if (!concurrentDictionary.TryGetValue(GetActionName(name), out actionWrapper))
                throw new InvalidOperationException("Unknown method");
            return actionWrapper;
        }

        private static string Version { get; } = string.Format("{0}.{1}", typeof(GetAttribute).Assembly.GetName().Version.Major, typeof(GetAttribute).Assembly.GetName().Version.Minor);

        public void SetProxyHandler(Func<string, IWebProxy> proxyFunc) => _proxyFunc = proxyFunc;

        private HttpRequestMessage CreateRequest(
          string path,
          string action,
          string contentType = "application/json")
        {
            HttpRequestMessage req = new HttpRequestMessage(new HttpMethod(action), CreateUrl(path))
            {
                Method = new HttpMethod(action)
            };
            req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(contentType));
            req.Headers.UserAgent.Add(new ProductInfoHeaderValue("stardust", Version));
            SetExtraHeaderValues(req);
            SetTimeoutValues(req);
            return req;
        }

        private string CreateUrl(string path) => !_trailingQueryString.IsNullOrWhiteSpace() ? NormalizeUrl(baseUri) + path + (path.Contains("?") ? "&" : "?") + _trailingQueryString : NormalizeUrl(baseUri) + path;

        private static string NormalizeUrl(string baseUrl)
        {
            Uri uri = new Uri(baseUrl);
            return uri.AbsolutePath + (uri.AbsolutePath.EndsWith("/") ? "" : "/");
        }

        private void SetExtraHeaderValues(HttpRequestMessage req)
        {
            foreach (KeyValuePair<string, string> additionalHeader in additionalHeaders)
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

        private static void SetTimeoutValues(HttpRequestMessage req)
        {
        }

        private HttpWebRequest AppendBody(
          byte[] buffer,
          HttpWebRequest req,
          ActionWrapper action)
        {
            if (buffer.IsEmpty())
                return req;
            req.GetRequestStream().Write(buffer, 0, buffer.Length);
            return req;
        }

        private async Task<HttpWebRequest> AppendBodyAsync(
          byte[] buffer,
          HttpWebRequest req,
          ActionWrapper action)
        {
            if (buffer.IsEmpty())
                return req;
            await (await req.GetRequestStreamAsync()).WriteAsync(buffer, 0, buffer.Length);
            return req;
        }

        private void AppendHeaders(
          ParameterWrapper[] parameters,
          HttpRequestMessage req,
          ActionWrapper action)
        {
            foreach (ParameterWrapper parameterWrapper in parameters.Where(p => p.In == InclutionTypes.Header))
                req.Headers.Add(string.Format("{0}", parameterWrapper.Name), parameterWrapper.value?.ToString().SanitizeHttpHeaderValue());
            if (action.CustomHandlers != null)
            {
                List<IHeaderHandler> source = new List<IHeaderHandler>();
                foreach (IHeaderInspector customHandler in action.CustomHandlers)
                    source.AddRange(customHandler.GetHandlers(_serviceLocator));
                foreach (IHeaderHandler headerHandler in source.OrderBy(f => f.ProcessingOrder))
                    headerHandler.SetHeader(req);
            }
            if (headerHandlers == null)
                return;
            foreach (IHeaderHandler headerHandler in headerHandlers)
                headerHandler.SetHeader(req);
        }

        private void XmlBodySerializer(WebRequest req, object val) => GetSerializer().Serialize(req, val);

        private ISerializer GetSerializer() => new Locator(_serviceLocator).GetServices<ISerializer>().SingleOrDefault(s => string.Equals(s.SerializationType, "xml", StringComparison.InvariantCultureIgnoreCase)) ?? throw new IndexOutOfRangeException("Could not find serializer");

        public async Task<ResultWrapper> ExecuteAsync(
          string name,
          ParameterWrapper[] parameters)
        {
            ActionWrapper action = GetAction(name);
            string path = BuildActionUrl(parameters, action);
            int cnt = 0;
            int waittime = action.Interval;
            while (cnt <= action.NumberOfRetries)
            {
                ++cnt;
                try
                {
                    ResultWrapper result = await CircuitBreakerContainer.GetCircuitBreaker(interfaceType).ExecuteAsync(baseUri + path, async () => await InvokeActionAsync(parameters, action, path), _serviceLocator);
                    if (result.Error != null)
                    {
                        if (result.Error is SuspendedDependencyException || !action.Retry || cnt > action.NumberOfRetries || !IsTransient(action, result.Error))
                            return result;
                        _serviceLocator.GetService<ILogger>()?.Error(result.Error);
                        _serviceLocator.GetService<ILogger>()?.Message(string.Format("Retrying action {0}, retry count {1}", action.Name, cnt));
                        waittime *= action.IncrementalRetry ? cnt : 1;
                        result.EndState();
                        await Task.Delay(waittime);
                    }
                    else
                    {
                        if (cnt > 1)
                            result.GetState().Extras.Add("retryCount", cnt);
                        return result;
                    }
                }
                catch (Exception ex)
                {
                    if (!action.Retry || cnt > action.NumberOfRetries || !IsTransient(action, ex))
                        throw;
                    await Task.Delay(action.IncrementalRetry ? cnt : action.Interval);
                }
            }
            throw new Exception("Should not get here!?!");
        }

        private bool IsTransient(ActionWrapper action, Exception exception)
        {
            if (action.ErrorCategorizer != null)
                return action.ErrorCategorizer.IsTransientError(exception);
            if (!(exception is WebException webException))
                return false;
            return new WebExceptionStatus[4]
            {
                WebExceptionStatus.ConnectionClosed,
                WebExceptionStatus.Timeout,
                WebExceptionStatus.RequestCanceled,
                WebExceptionStatus.ConnectFailure
            }.Contains(webException.Status);
        }

        private async Task<ResultWrapper> InvokeActionAsync(
          ParameterWrapper[] parameters,
          ActionWrapper action,
          string path)
        {
            HttpRequestMessage req = await CreateActionRequestAsync(parameters, action, path);
            HttpResponseMessage response = null;
            ResultWrapper errorResult;
            try
            {
                response = await GetClient().SendAsync(req);
                if (!response.IsSuccessStatusCode)
                    throw new HttpRequestException(response.ReasonPhrase);
                EnsureActionId(req, response);
                GetHeaderValues(action, response);
                ResultWrapper result = await CreateResult(action, response);
                result.ActionId = req.ActionId();
                return result;
            }
            catch (HttpRequestException ex)
            {
                EnsureActionId(req, response);
                GetHeaderValues(action, response);
                HttpResponseMessage response1 = response;
                ActionWrapper action1 = action;
                errorResult = await HandleWebException(ex, response1, action1);
            }
            catch (Exception ex)
            {
                errorResult = HandleGenericException(ex);
            }
            finally
            {
                HttpRequestMessage self1 = req;
                if (self1 != null)
                    self1.TryDispose();
                HttpResponseMessage self2 = response;
                if (self2 != null)
                    self2.TryDispose();
            }
            errorResult.ActionId = req.ActionId();
            return errorResult;
        }

        private static void EnsureActionId(HttpRequestMessage req, HttpResponseMessage resp)
        {
            bool? nullable;
            if (resp == null)
            {
                nullable = new bool?();
            }
            else
            {
                HttpResponseHeaders headers = resp.Headers;
                nullable = headers != null ? !headers.Contains("sd-ActionId") : new bool?();
            }
            if (!nullable.GetValueOrDefault() || resp == null)
                return;
            resp.Headers?.Add("sd-ActionId", req.ActionId());
        }

        public T Invoke<T>(string name, ParameterWrapper[] parameters)
        {
            ResultWrapper result = Execute(name, parameters);
            Action<Dictionary<string, object>> extras = Extras;
            if (extras != null)
                extras(result.GetState().Extras);
            result.EndState();
            if (result.Error == null)
                return (T)result.Value;
            CreateException(name, result);
            return default(T);
        }

        public async Task<T> InvokeAsync<T>(string name, ParameterWrapper[] parameters)
        {
            ResultWrapper result = await ExecuteAsync(GetActionName(name), parameters);
            Action<Dictionary<string, object>> extras = Extras;
            if (extras != null)
                extras(result.GetState().Extras);
            StateHelper.EndState(result.ActionId);
            if (typeof(T) == typeof(void))
                return default(T);
            if (result.Error == null)
                return (T)result.Value;
            CreateException(name, result);
            return default(T);
        }

        private void CreateException(string name, ResultWrapper result)
        {
            ActionWrapper action = GetAction(name);
            if (result != null && result.Status == (HttpStatusCode)429)
                throw new RestWrapperException(result?.StatusMessage, (HttpStatusCode)429, new ThrottledRequestException(result?.Error));
            IErrorHandler errorHandler = GetErrorHandler(action);
            if (errorHandler != null)
                throw errorHandler.ProduceClientException(result.StatusMessage, result.Status, result.Error, result.Value as string);
            if (result != null && result.Value != null)
                throw new RestWrapperException(result.StatusMessage, result.Status, result.Value, result.Error);
            throw new RestWrapperException(result?.StatusMessage, result != null ? result.Status : HttpStatusCode.Unused, result?.Error);
        }

        private IErrorHandler GetErrorHandler(ActionWrapper action)
        {
            IErrorHandler service = _serviceLocator.GetService<IErrorHandler>();
            ErrorHandlerAttribute errorHandler = action?.ErrorHandler;
            return service == null || errorHandler == null ? (service == null ? errorHandler?.ErrorHandler(_serviceLocator) : service) : new AggregateHandler(service, errorHandler?.ErrorHandler(_serviceLocator));
        }

        public async Task InvokeVoidAsync(string name, ParameterWrapper[] parameters)
        {
            ResultWrapper result = await ExecuteAsync(GetActionName(name), parameters);
            StateHelper.EndState(result.ActionId);
            if (result.Error == null)
                return;
            CreateException(name, result);
        }

        public void InvokeVoid(string name, ParameterWrapper[] parameters)
        {
            ResultWrapper result = Execute(name, parameters);
            StateHelper.EndState(result.ActionId);
            if (result.Error == null)
                return;
            CreateException(name, result);
        }

        protected ParameterWrapper[] GetParameters(
          string name,
          params object[] parameters)
        {
            ConcurrentDictionary<string, ActionWrapper> concurrentDictionary;
            if (!cache.TryGetValue(interfaceType, out concurrentDictionary))
                throw new InvalidOperationException("Invalid interface type");
            ActionWrapper actionWrapper;
            if (!concurrentDictionary.TryGetValue(GetActionName(name), out actionWrapper))
                throw new InvalidOperationException("Invalid action");
            int index = 0;
            List<ParameterWrapper> parameterWrapperList = new List<ParameterWrapper>();
            foreach (object parameter1 in parameters)
            {
                ParameterWrapper parameter2 = actionWrapper.Parameters[index];
                parameterWrapperList.Add(parameter2.Create(parameter1));
                ++index;
            }
            return parameterWrapperList.ToArray();
        }

        public IServiceProvider ServiceLocator => _serviceLocator;

        public void AppendTrailingQueryString(string queryStringSegment) => _trailingQueryString = _trailingQueryString.ContainsCharacters() ? "&" : queryStringSegment ?? "";

        public void SetPathVersion(string version)
        {
            if (_pathVersionSet)
                return;
            baseUri = baseUri + (baseUri.EndsWith("/") ? "" : "/") + version;
            _pathVersionSet = true;
        }
    }
}