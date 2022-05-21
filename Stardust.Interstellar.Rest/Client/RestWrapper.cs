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

        public void SetHttpHeader(string name, string value) => this.additionalHeaders.TryAdd(name, value);

        internal static ConcurrentDictionary<Type, ConcurrentDictionary<string, ActionWrapper>> Cache() => RestWrapper.cache;

        public void SetBaseUrl(string url) => this.baseUri = url;

        public Action<Dictionary<string, object>> Extras { get; internal set; }

        protected RestWrapper(
          IAuthenticationHandler authenticationHandler,
          IHeaderHandlerFactory headerHandlers,
          TypeWrapper interfaceType,
          IServiceProvider serviceLocator)
        {
            this.authenticationHandler = authenticationHandler;
            this._serviceLocator = serviceLocator;
            this.headerHandlers = headerHandlers?.GetHandlers(this._serviceLocator) ?? (IEnumerable<IHeaderHandler>)new List<IHeaderHandler>();
            this.interfaceType = interfaceType.Type;
            this.InitializeClient(this.interfaceType);
            this.cookieContainer = this._serviceLocator.GetService<CookieContainer>() ?? new CookieContainer();
            this._logger = serviceLocator.GetService<ILogger>();
        }

        private static IRoutePrefix GetRoutePrefix(Type interfaceType)
        {
            ApiAttribute apiAttribute = interfaceType.GetCustomAttribute<ApiAttribute>();
            if (apiAttribute == null)
            {
                Type element = ((IEnumerable<Type>)interfaceType.GetInterfaces()).FirstOrDefault<Type>();
                apiAttribute = (object)element != null ? element.GetCustomAttribute<ApiAttribute>() : (ApiAttribute)null;
            }
            IRoutePrefix routePrefix = (IRoutePrefix)apiAttribute;
            if (routePrefix == null)
            {
                IRoutePrefixAttribute iroutePrefixAttribute = interfaceType.GetCustomAttribute<IRoutePrefixAttribute>();
                if (iroutePrefixAttribute == null)
                {
                    Type element = ((IEnumerable<Type>)interfaceType.GetInterfaces()).FirstOrDefault<Type>();
                    iroutePrefixAttribute = (object)element != null ? element.GetCustomAttribute<IRoutePrefixAttribute>() : (IRoutePrefixAttribute)null;
                }
                routePrefix = (IRoutePrefix)iroutePrefixAttribute;
            }
            return routePrefix;
        }

        public void InitializeClient(Type interfaceType)
        {
            ConcurrentDictionary<string, ActionWrapper> concurrentDictionary1;
            if (RestWrapper.cache.TryGetValue(interfaceType, out concurrentDictionary1))
                return;
            ConcurrentDictionary<string, ActionWrapper> concurrentDictionary2 = new ConcurrentDictionary<string, ActionWrapper>();
            IRoutePrefix routePrefix = RestWrapper.GetRoutePrefix(interfaceType);
            this._errorInterceptor = interfaceType.GetCustomAttribute<ErrorHandlerAttribute>();
            this._serviceLocator.GetService<ILogger>()?.Message("Initializing client " + interfaceType.Name);
            RetryAttribute customAttribute = interfaceType.GetCustomAttribute<RetryAttribute>();
            if (interfaceType.GetCustomAttribute<CircuitBreakerAttribute>() != null)
                CircuitBreakerContainer.Register(interfaceType, (ICircuitBreaker)new Stardust.Interstellar.Rest.Client.CircuitBreaker.CircuitBreaker(interfaceType.GetCustomAttribute<CircuitBreakerAttribute>(), this._serviceLocator)
                {
                    ServiceName = interfaceType.FullName
                });
            else
                CircuitBreakerContainer.Register(interfaceType, (ICircuitBreaker)new NullBreaker());
            foreach (MethodInfo methodInfo in interfaceType.GetMethods().Length == 0 ? ((IEnumerable<Type>)interfaceType.GetInterfaces()).First<Type>().GetMethods() : interfaceType.GetMethods())
            {
                try
                {
                    RetryAttribute retryAttribute = methodInfo.GetCustomAttribute<RetryAttribute>() ?? customAttribute;
                    this._serviceLocator.GetService<ILogger>()?.Message("Initializing client action " + interfaceType.Name + "." + methodInfo.Name);
                    IRoute template = (IRoute)methodInfo.GetCustomAttribute<IRouteAttribute>() ?? (IRoute)methodInfo.GetCustomAttribute<VerbAttribute>();
                    string actionName = this.GetActionName(methodInfo);
                    List<VerbAttribute> list = methodInfo.GetCustomAttributes(true).OfType<VerbAttribute>().ToList<VerbAttribute>();
                    ActionWrapper action = new ActionWrapper()
                    {
                        Name = actionName,
                        ReturnType = methodInfo.ReturnType,
                        RouteTemplate = ExtensionsFactory.GetRouteTemplate(routePrefix, template, methodInfo, this._serviceLocator),
                        Parameters = new List<ParameterWrapper>()
                    };
                    MethodInfo method = methodInfo;
                    IServiceProvider serviceLocator = this._serviceLocator;
                    List<HttpMethod> httpMethods = ExtensionsFactory.GetHttpMethods(list, method, serviceLocator);
                    List<IHeaderInspector> headerInspectors = ExtensionsFactory.GetHeaderInspectors(methodInfo, this._serviceLocator);
                    action.UseXml = methodInfo.GetCustomAttributes().OfType<UseXmlAttribute>().Any<UseXmlAttribute>();
                    action.CustomHandlers = headerInspectors.Where<IHeaderInspector>((Func<IHeaderInspector, bool>)(h => this.headerHandlers.All<IHeaderHandler>((Func<IHeaderHandler, bool>)(parent => parent.GetType() != h.GetType())))).ToList<IHeaderInspector>();
                    action.ErrorHandler = this._errorInterceptor;
                    action.Actions = httpMethods;
                    if (retryAttribute != null)
                    {
                        action.Retry = true;
                        action.Interval = retryAttribute.RetryInterval;
                        action.NumberOfRetries = retryAttribute.NumberOfRetries;
                        action.IncrementalRetry = retryAttribute.IncremetalWait;
                        if (retryAttribute.ErrorCategorizer != (Type)null)
                            action.ErrorCategorizer = (IErrorCategorizer)Activator.CreateInstance(retryAttribute.ErrorCategorizer, (object)this._serviceLocator);
                    }
                    ExtensionsFactory.BuildParameterInfo(methodInfo, action, this._serviceLocator);
                    concurrentDictionary2.TryAdd(action.Name, action);
                }
                catch (Exception ex)
                {
                    this._logger?.Error(ex);
                    throw;
                }
            }
            if (RestWrapper.cache.TryGetValue(interfaceType, out concurrentDictionary1))
                return;
            RestWrapper.cache.TryAdd(interfaceType, concurrentDictionary2);
        }

        protected string GetActionName(MethodInfo methodInfo) => RestWrapper.GetActionName(methodInfo.Name);

        internal static string GetActionName(string actionName)
        {
            if (actionName.EndsWith("Async"))
                actionName = actionName.Replace("Async", "");
            return actionName;
        }

        public ResultWrapper Execute(string name, ParameterWrapper[] parameters)
        {
            ActionWrapper action = this.GetAction(name);
            string path = RestWrapper.BuildActionUrl(parameters, action);
            int num = 0;
            int interval = action.Interval;
            while (num <= action.NumberOfRetries)
            {
                ++num;
                try
                {
                    ResultWrapper result = CircuitBreakerContainer.GetCircuitBreaker(this.interfaceType).Execute(this.baseUri + path, (Func<ResultWrapper>)(() => this.InvokeAction(name, parameters, action, path)), this._serviceLocator);
                    if (result.Error != null)
                    {
                        if (result.Error is SuspendedDependencyException || !action.Retry || num > action.NumberOfRetries || !this.IsTransient(action, result.Error))
                            return result;
                        this._serviceLocator.GetService<ILogger>()?.Error(result.Error);
                        this._serviceLocator.GetService<ILogger>()?.Message(string.Format("Retrying action {0}, retry count {1}", (object)action.Name, (object)num));
                        interval *= action.IncrementalRetry ? num : 1;
                        result.EndState();
                        Thread.Sleep(interval);
                    }
                    else
                    {
                        if (num > 1)
                            result.GetState().Extras.Add("retryCount", (object)num);
                        return result;
                    }
                }
                catch (Exception ex)
                {
                    this._logger?.Error(ex);
                    if (!action.Retry || num > action.NumberOfRetries || !this.IsTransient(action, ex))
                        throw;
                    else
                        Thread.Sleep(action.IncrementalRetry ? num : action.Interval);
                }
            }
            throw new Exception("Should not get here!?!");
        }

        private HttpClient GetClient()
        {
            HttpClient httpClient1;
            if (RestWrapper._clients.TryGetValue(this.baseUri.ToLower(), out httpClient1))
                return httpClient1;
            HttpClient httpClient2 = new HttpClient()
            {
                BaseAddress = new Uri(this.baseUri),
                Timeout = TimeSpan.FromSeconds((double)(ClientGlobalSettings.Timeout ?? 100))
            };
            RestWrapper._clients.TryAdd(this.baseUri.ToLower(), httpClient2);
            return httpClient2;
        }

        private ResultWrapper InvokeAction(
          string name,
          ParameterWrapper[] parameters,
          ActionWrapper action,
          string path)
        {
            return Task.Run<ResultWrapper>((Func<Task<ResultWrapper>>)(async () => await this.InvokeActionAsync(parameters, action, path))).Result;
        }

        private void GetHeaderValues(ActionWrapper action, HttpResponseMessage response)
        {
            if (response == null)
                return;
            List<IHeaderHandler> source = new List<IHeaderHandler>();
            foreach (IHeaderInspector customHandler in action.CustomHandlers)
                source.AddRange((IEnumerable<IHeaderHandler>)customHandler.GetHandlers(this._serviceLocator));
            foreach (IHeaderHandler headerHandler in (IEnumerable<IHeaderHandler>)source.OrderBy<IHeaderHandler, int>((Func<IHeaderHandler, int>)(f => f.ProcessingOrder)))
                headerHandler.GetHeader(response);
        }

        private static ResultWrapper HandleGenericException(Exception ex) => new ResultWrapper()
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
                string errorBody = await RestWrapper.TryGetErrorBody(action, response);
                return new ResultWrapper()
                {
                    Status = response.StatusCode,
                    StatusMessage = response.ReasonPhrase,
                    Error = (Exception)webError,
                    Value = (object)errorBody
                };
            }
            return new ResultWrapper()
            {
                Status = HttpStatusCode.BadGateway,
                StatusMessage = webError.Message,
                Error = (Exception)new WebException(webError.Message)
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
            return (string)null;
        }

        private async Task<ResultWrapper> CreateResult(
          ActionWrapper action,
          HttpResponseMessage response)
        {
            Type type = typeof(Task).IsAssignableFrom(action.ReturnType) ? ((IEnumerable<Type>)action.ReturnType.GetGenericArguments()).FirstOrDefault<Type>() : action.ReturnType;
            if (type == typeof(void) || type == (Type)null)
                return new ResultWrapper()
                {
                    Type = typeof(void),
                    IsVoid = true,
                    Value = (object)null,
                    Status = response.StatusCode,
                    StatusMessage = response.ReasonPhrase,
                    ActionId = response.ActionId()
                };
            object resultFromResponse = await this.GetResultFromResponse(action, response, type);
            return new ResultWrapper()
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
                ISerializer serializer = this.GetSerializer();
                obj = serializer.Deserialize(await response.Content.ReadAsStreamAsync(), type);
                serializer = (ISerializer)null;
            }
            else
            {
                using (JsonTextReader jsonTextReader = new JsonTextReader((TextReader)new StreamReader(await response.Content.ReadAsStreamAsync())))
                    obj = this.CreateJsonSerializer(type).Deserialize((JsonReader)jsonTextReader, type);
            }
            return obj;
        }

        private JsonSerializer CreateJsonSerializer(Type getType)
        {
            JsonSerializerSettings settings = (JsonSerializerSettings)null;
            if (getType != (Type)null)
                settings = getType.GetClientSerializationSettings();
            if (settings == null)
                settings = this.interfaceType.GetClientSerializationSettings();
            return settings != null ? JsonSerializer.Create(settings) : JsonSerializer.Create();
        }

        private HttpRequestMessage CreateActionRequest(
          ParameterWrapper[] parameters,
          ActionWrapper action,
          string path)
        {
            HttpRequestMessage actionRequestBase = this.CreateActionRequestBase(parameters, action, path);
            byte[] body = this.PrepareBody(parameters, actionRequestBase, action);
            if (this.authenticationHandler == null)
                this.authenticationHandler = this._serviceLocator.GetService<IAuthenticationHandler>();
            this.authenticationHandler?.BodyData(body);
            if (this.RunAuthProviderBeforeAppendingBody)
                this.authenticationHandler?.Apply(actionRequestBase);
            if (!this.RunAuthProviderBeforeAppendingBody)
                this.authenticationHandler?.Apply(actionRequestBase);
            return actionRequestBase;
        }

        public bool RunAuthProviderBeforeAppendingBody
        {
            get => this._runAuthProviderBeforeAppendingBody ?? ProxyFactory.RunAuthProviderBeforeAppendingBody;
            set => this._runAuthProviderBeforeAppendingBody = new bool?(value);
        }

        private async Task<HttpRequestMessage> CreateActionRequestAsync(
          ParameterWrapper[] parameters,
          ActionWrapper action,
          string path)
        {
            HttpRequestMessage req = this.CreateActionRequestBase(parameters, action, path);
            byte[] body = this.PrepareBody(parameters, req, action);
            if (this.authenticationHandler == null)
                this.authenticationHandler = this._serviceLocator.GetService<IAuthenticationHandler>();
            this.authenticationHandler?.BodyData(body);
            if (this.RunAuthProviderBeforeAppendingBody && this.authenticationHandler != null)
                await this.authenticationHandler.ApplyAsync(req);
            if (req.Method != HttpMethod.Get && req.Method != HttpMethod.Head && req.Method != HttpMethod.Options && req.Method != HttpMethod.Trace)
            {
                req.Content = (HttpContent)new ByteArrayContent(body ?? new byte[0]);
                req.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            }
            if (!this.RunAuthProviderBeforeAppendingBody && this.authenticationHandler != null)
                await this.authenticationHandler.ApplyAsync(req);
            HttpRequestMessage httpRequestMessage = req;
            req = (HttpRequestMessage)null;
            body = (byte[])null;
            return httpRequestMessage;
        }

        private byte[] PrepareBody(
          ParameterWrapper[] parameters,
          HttpRequestMessage req,
          ActionWrapper action)
        {
            if (!((IEnumerable<ParameterWrapper>)parameters).Any<ParameterWrapper>((Func<ParameterWrapper, bool>)(p => p.In == InclutionTypes.Body)))
                return (byte[])null;
            if (((IEnumerable<ParameterWrapper>)parameters).Count<ParameterWrapper>((Func<ParameterWrapper, bool>)(p => p.In == InclutionTypes.Body)) > 1)
                return this.Serialize((object)((IEnumerable<ParameterWrapper>)parameters).Where<ParameterWrapper>((Func<ParameterWrapper, bool>)(p => p.In == InclutionTypes.Body)).Select<ParameterWrapper, object>((Func<ParameterWrapper, object>)(p => p.value)).ToList<object>(), action);
            ParameterWrapper parameterWrapper = ((IEnumerable<ParameterWrapper>)parameters).Single<ParameterWrapper>((Func<ParameterWrapper, bool>)(p => p.In == InclutionTypes.Body));
            object base64String = parameterWrapper.value;
            if (parameterWrapper.value != null && parameterWrapper.value?.GetType() == typeof(byte[]))
                base64String = (object)Convert.ToBase64String((byte[])parameterWrapper.value);
            return this.Serialize(base64String, action);
        }

        private byte[] Serialize(object value, ActionWrapper action)
        {
            if (action.UseXml)
                return this.GetSerializer().Serialize(value);
            using (MemoryStream memoryStream = new MemoryStream())
            {
                using (StreamWriter streamWriter = new StreamWriter((Stream)memoryStream))
                {
                    using (JsonTextWriter jsonTextWriter = new JsonTextWriter((TextWriter)streamWriter))
                        this.CreateJsonSerializer(value?.GetType()).Serialize((JsonWriter)jsonTextWriter, value);
                }
                return memoryStream.ToArray();
            }
        }

        private HttpRequestMessage CreateActionRequestBase(
          ParameterWrapper[] parameters,
          ActionWrapper action,
          string path)
        {
            string action1 = action.Actions.First<HttpMethod>().ToString();
            HttpRequestMessage httpRequestMessage = action.UseXml ? this.CreateRequest(path, action1, "application/xml") : this.CreateRequest(path, action1);
            httpRequestMessage.Headers.Add("sd-ActionId", Guid.NewGuid().ToString());
            httpRequestMessage.InitializeState();
            httpRequestMessage.GetState().Extras.Add("serviceRoot", (object)this.baseUri);
            this.AppendHeaders(parameters, httpRequestMessage, action);
            return httpRequestMessage;
        }

        private static string BuildActionUrl(ParameterWrapper[] parameters, ActionWrapper action)
        {
            if (action == null)
                Console.WriteLine("no action definition found ?!?!");
            string str = action?.RouteTemplate ?? "";
            List<string> source = new List<string>();
            if (parameters == null)
                return str;
            foreach (ParameterWrapper parameterWrapper in ((IEnumerable<ParameterWrapper>)parameters).Where<ParameterWrapper>((Func<ParameterWrapper, bool>)(p => p.In == InclutionTypes.Path || p.In == InclutionTypes.Query)))
            {
                if (str.Contains("{" + parameterWrapper.Name + "}") && parameterWrapper.In == InclutionTypes.Path)
                    str = str.Replace("{" + parameterWrapper.Name + "}", Uri.EscapeDataString(parameterWrapper?.value?.ToString() ?? ""));
                else
                    source.Add(parameterWrapper.Name + "=" + HttpUtility.UrlEncode(parameterWrapper?.value?.ToString() ?? ""));
            }
            if (source.Any<string>())
                str = str + "?" + string.Join("&", (IEnumerable<string>)source);
            if (str.StartsWith("/"))
                str = str.Substring(1);
            return str;
        }

        public static bool DisableProxy { get; set; }

        private ActionWrapper GetAction(string name)
        {
            ConcurrentDictionary<string, ActionWrapper> concurrentDictionary;
            if (!RestWrapper.cache.TryGetValue(this.interfaceType, out concurrentDictionary))
                throw new InvalidOperationException("Unknown interface type");
            ActionWrapper actionWrapper;
            if (!concurrentDictionary.TryGetValue(RestWrapper.GetActionName(name), out actionWrapper))
                throw new InvalidOperationException("Unknown method");
            return actionWrapper;
        }

        private static string Version { get; } = string.Format("{0}.{1}", (object)typeof(GetAttribute).Assembly.GetName().Version.Major, (object)typeof(GetAttribute).Assembly.GetName().Version.Minor);

        public void SetProxyHandler(Func<string, IWebProxy> proxyFunc) => this._proxyFunc = proxyFunc;

        private HttpRequestMessage CreateRequest(
          string path,
          string action,
          string contentType = "application/json")
        {
            HttpRequestMessage req = new HttpRequestMessage(new HttpMethod(action), this.CreateUrl(path))
            {
                Method = new HttpMethod(action)
            };
            req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(contentType));
            req.Headers.UserAgent.Add(new ProductInfoHeaderValue("stardust", RestWrapper.Version));
            this.SetExtraHeaderValues(req);
            RestWrapper.SetTimeoutValues(req);
            return req;
        }

        private string CreateUrl(string path) => !this._trailingQueryString.IsNullOrWhiteSpace() ? RestWrapper.NormalizeUrl(this.baseUri) + path + (path.Contains("?") ? "&" : "?") + this._trailingQueryString : RestWrapper.NormalizeUrl(this.baseUri) + path;

        private static string NormalizeUrl(string baseUrl)
        {
            Uri uri = new Uri(baseUrl);
            return uri.AbsolutePath + (uri.AbsolutePath.EndsWith("/") ? "" : "/");
        }

        private void SetExtraHeaderValues(HttpRequestMessage req)
        {
            foreach (KeyValuePair<string, string> additionalHeader in this.additionalHeaders)
            {
                try
                {
                    req.Headers.Add(additionalHeader.Key, additionalHeader.Value);
                }
                catch (Exception ex)
                {
                    this._serviceLocator.GetService<ILogger>()?.Error(ex);
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
            foreach (ParameterWrapper parameterWrapper in ((IEnumerable<ParameterWrapper>)parameters).Where<ParameterWrapper>((Func<ParameterWrapper, bool>)(p => p.In == InclutionTypes.Header)))
                req.Headers.Add(string.Format("{0}", (object)parameterWrapper.Name), parameterWrapper.value?.ToString().SanitizeHttpHeaderValue());
            if (action.CustomHandlers != null)
            {
                List<IHeaderHandler> source = new List<IHeaderHandler>();
                foreach (IHeaderInspector customHandler in action.CustomHandlers)
                    source.AddRange((IEnumerable<IHeaderHandler>)customHandler.GetHandlers(this._serviceLocator));
                foreach (IHeaderHandler headerHandler in (IEnumerable<IHeaderHandler>)source.OrderBy<IHeaderHandler, int>((Func<IHeaderHandler, int>)(f => f.ProcessingOrder)))
                    headerHandler.SetHeader(req);
            }
            if (this.headerHandlers == null)
                return;
            foreach (IHeaderHandler headerHandler in this.headerHandlers)
                headerHandler.SetHeader(req);
        }

        private void XmlBodySerializer(WebRequest req, object val) => this.GetSerializer().Serialize(req, val);

        private ISerializer GetSerializer() => new Locator(this._serviceLocator).GetServices<ISerializer>().SingleOrDefault<ISerializer>((Func<ISerializer, bool>)(s => string.Equals(s.SerializationType, "xml", StringComparison.InvariantCultureIgnoreCase))) ?? throw new IndexOutOfRangeException("Could not find serializer");

        public async Task<ResultWrapper> ExecuteAsync(
          string name,
          ParameterWrapper[] parameters)
        {
            ActionWrapper action = this.GetAction(name);
            string path = RestWrapper.BuildActionUrl(parameters, action);
            int cnt = 0;
            int waittime = action.Interval;
            while (cnt <= action.NumberOfRetries)
            {
                ++cnt;
                try
                {
                    ResultWrapper result = await CircuitBreakerContainer.GetCircuitBreaker(this.interfaceType).ExecuteAsync(this.baseUri + path, (Func<Task<ResultWrapper>>)(async () => await this.InvokeActionAsync(parameters, action, path)), this._serviceLocator);
                    if (result.Error != null)
                    {
                        if (result.Error is SuspendedDependencyException || !action.Retry || cnt > action.NumberOfRetries || !this.IsTransient(action, result.Error))
                            return result;
                        this._serviceLocator.GetService<ILogger>()?.Error(result.Error);
                        this._serviceLocator.GetService<ILogger>()?.Message(string.Format("Retrying action {0}, retry count {1}", (object)action.Name, (object)cnt));
                        waittime *= action.IncrementalRetry ? cnt : 1;
                        result.EndState();
                        await Task.Delay(waittime);
                    }
                    else
                    {
                        if (cnt > 1)
                            result.GetState().Extras.Add("retryCount", (object)cnt);
                        return result;
                    }
                }
                catch (Exception ex)
                {
                    if (!action.Retry || cnt > action.NumberOfRetries || !this.IsTransient(action, ex))
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
            return ((IEnumerable<WebExceptionStatus>)new WebExceptionStatus[4]
            {
        WebExceptionStatus.ConnectionClosed,
        WebExceptionStatus.Timeout,
        WebExceptionStatus.RequestCanceled,
        WebExceptionStatus.ConnectFailure
            }).Contains<WebExceptionStatus>(webException.Status);
        }

        private async Task<ResultWrapper> InvokeActionAsync(
          ParameterWrapper[] parameters,
          ActionWrapper action,
          string path)
        {
            HttpRequestMessage req = await this.CreateActionRequestAsync(parameters, action, path);
            HttpResponseMessage response = (HttpResponseMessage)null;
            ResultWrapper errorResult;
            try
            {
                response = await this.GetClient().SendAsync(req);
                if (!response.IsSuccessStatusCode)
                    throw new HttpRequestException(response.ReasonPhrase);
                RestWrapper.EnsureActionId(req, response);
                this.GetHeaderValues(action, response);
                ResultWrapper result = await this.CreateResult(action, response);
                result.ActionId = req.ActionId();
                return result;
            }
            catch (HttpRequestException ex)
            {
                RestWrapper.EnsureActionId(req, response);
                this.GetHeaderValues(action, response);
                HttpResponseMessage response1 = response;
                ActionWrapper action1 = action;
                errorResult = await RestWrapper.HandleWebException(ex, response1, action1);
            }
            catch (Exception ex)
            {
                errorResult = RestWrapper.HandleGenericException(ex);
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
                nullable = headers != null ? new bool?(!headers.Contains("sd-ActionId")) : new bool?();
            }
            if (!nullable.GetValueOrDefault() || resp == null)
                return;
            resp.Headers?.Add("sd-ActionId", req.ActionId());
        }

        public T Invoke<T>(string name, ParameterWrapper[] parameters)
        {
            ResultWrapper result = this.Execute(name, parameters);
            Action<Dictionary<string, object>> extras = this.Extras;
            if (extras != null)
                extras((Dictionary<string, object>)result.GetState().Extras);
            result.EndState();
            if (result.Error == null)
                return (T)result.Value;
            this.CreateException(name, result);
            return default(T);
        }

        public async Task<T> InvokeAsync<T>(string name, ParameterWrapper[] parameters)
        {
            ResultWrapper result = await this.ExecuteAsync(RestWrapper.GetActionName(name), parameters);
            Action<Dictionary<string, object>> extras = this.Extras;
            if (extras != null)
                extras((Dictionary<string, object>)result.GetState().Extras);
            StateHelper.EndState(result.ActionId);
            if (typeof(T) == typeof(void))
                return default(T);
            if (result.Error == null)
                return (T)result.Value;
            this.CreateException(name, result);
            return default(T);
        }

        private void CreateException(string name, ResultWrapper result)
        {
            ActionWrapper action = this.GetAction(name);
            if (result != null && result.Status == (HttpStatusCode)429)
                throw new RestWrapperException(result?.StatusMessage, (HttpStatusCode)429, (Exception)new ThrottledRequestException(result?.Error));
            IErrorHandler errorHandler = this.GetErrorHandler(action);
            if (errorHandler != null)
                throw errorHandler.ProduceClientException(result.StatusMessage, result.Status, result.Error, result.Value as string);
            if (result != null && result.Value != null)
                throw new RestWrapperException(result.StatusMessage, result.Status, result.Value, result.Error);
            throw new RestWrapperException(result?.StatusMessage, result != null ? result.Status : HttpStatusCode.Unused, result?.Error);
        }

        private IErrorHandler GetErrorHandler(ActionWrapper action)
        {
            IErrorHandler service = this._serviceLocator.GetService<IErrorHandler>();
            ErrorHandlerAttribute errorHandler = action?.ErrorHandler;
            return service == null || errorHandler == null ? (service == null ? errorHandler?.ErrorHandler(this._serviceLocator) : service) : (IErrorHandler)new AggregateHandler(service, errorHandler?.ErrorHandler(this._serviceLocator));
        }

        public async Task InvokeVoidAsync(string name, ParameterWrapper[] parameters)
        {
            ResultWrapper result = await this.ExecuteAsync(RestWrapper.GetActionName(name), parameters);
            StateHelper.EndState(result.ActionId);
            if (result.Error == null)
                return;
            this.CreateException(name, result);
        }

        public void InvokeVoid(string name, ParameterWrapper[] parameters)
        {
            ResultWrapper result = this.Execute(name, parameters);
            StateHelper.EndState(result.ActionId);
            if (result.Error == null)
                return;
            this.CreateException(name, result);
        }

        protected ParameterWrapper[] GetParameters(
          string name,
          params object[] parameters)
        {
            ConcurrentDictionary<string, ActionWrapper> concurrentDictionary;
            if (!RestWrapper.cache.TryGetValue(this.interfaceType, out concurrentDictionary))
                throw new InvalidOperationException("Invalid interface type");
            ActionWrapper actionWrapper;
            if (!concurrentDictionary.TryGetValue(RestWrapper.GetActionName(name), out actionWrapper))
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

        public IServiceProvider ServiceLocator => this._serviceLocator;

        public void AppendTrailingQueryString(string queryStringSegment) => this._trailingQueryString = this._trailingQueryString.ContainsCharacters() ? "&" : queryStringSegment ?? "";

        public void SetPathVersion(string version)
        {
            if (this._pathVersionSet)
                return;
            this.baseUri = this.baseUri + (this.baseUri.EndsWith("/") ? "" : "/") + version;
            this._pathVersionSet = true;
        }
    }
}