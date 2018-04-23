# Stardust.Interstellar.Rest


This is a tool for creating webapi controllers and clients based upon an interface that is mostly decorated by the common annotations you are used to. It is mainly intended to be used inside the organization 0r the product. But workes well to provide .net clients to the public as well.

When both the client and the service are located in the same solution you will have ctrl-F12 support from the client code.

Unit testing and reuse are simplified. All controllers are implemented in an consistent way, no surprises.

Add the nuget: Install-Package Stardust.Interstellar.Rest

Sample service definition interface
```CS
    [IRoutePrefix("api")] //Custom: as the RoutePrefix attribute only supports classes, this is one to one
    [CallingMachineName]
    public interface ITestApi
    {
        [Route("test/{id}")]
        [HttpGet]
        string Apply1([In(InclutionTypes.Path)] string id, [In(InclutionTypes.Path)]string name);// the In attribute supports more than the basic webapi attributes (but you can use them; FomeUri and FromBody)

        [Route("test2/{id}")]
        [HttpGet]
        string Apply2([In(InclutionTypes.Path)] string id, [In(InclutionTypes.Path)]string name, [In(InclutionTypes.Header)]string item3);

        [Route("test3/{id}")]
        [HttpGet]
        string Apply3([In(InclutionTypes.Path)] string id, [In(InclutionTypes.Path)]string name, [In(InclutionTypes.Header)]string item3, [In(InclutionTypes.Header)]string item4);

        [Route("put1/{id}")]
        [HttpPut]
        void Put([In(InclutionTypes.Path)] string id, [In(InclutionTypes.Body)] DateTime timestamp);

        [Route("test5/{id}")]
        [HttpGet]
        Task<StringWrapper> ApplyAsync([In(InclutionTypes.Path)] string id, [In(InclutionTypes.Path)]string name, [In(InclutionTypes.Path)]string item3, [In(InclutionTypes.Path)]string item4);

        [Route("put2/{id}")]
        [HttpPut]
        Task PutAsync([In(InclutionTypes.Path)] string id, [In(InclutionTypes.Body)] DateTime timestamp);
    }
```
Creating a service proxy
```CS

        var service = ProxyFactory.CreateInstance<ITestApi>("http://localhost/Stardust.Interstellar.Test/",
                    extras =>
                        {
                            foreach (var extra in extras)
                            {
                                output.WriteLine($"{extra.Key}:{extra.Value}");
                            }
                        });
        try
        {
            var res =await service.ApplyAsync("101", "SampleService", "Hello", "Sample");
            output.WriteLine(res.Value);
            // outputs: 101-SampleService-Hello-Sample
        }
        catch (Exception ex)
        {
            throw;
        }
```
Optional: Setting JsonSerializerSettings to service definitions or message types 

```CS

new JsonSerializerSettings
                              {
                                  DateFormatHandling = DateFormatHandling.MicrosoftDateFormat,
                                  DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate,
                                  NullValueHandling = NullValueHandling.Include,
                                  Formatting = Formatting.Indented
                              }.AddClientSerializer<IMyService>().AddClientSerializer<SomeMessageType>;
```


Creating the service implementation (just implement it as you implement any other interface):
```CS
    public class TestApiImp : ITestApi
    {
        public string Apply1(string id, string name)
        {
            return string.Join("-", id, name);
        }

        public string Apply2(string id, string name, string item3)
        {
            return string.Join("-", id, name,item3);
        }

        public string Apply3(string id, string name, string item3, string item4)
        {
            return string.Join("-", id, name,item3,item4);
        }

        public void Put(string id, DateTime timestamp)
        {
            return;
        }

        public Task<StringWrapper> ApplyAsync(string id, string name, string item3, string item4)
        {
            return Task.FromResult(new StringWrapper {Value = string.Join("-", id, name, item3, item4) });
        }

        public Task PutAsync(string id, DateTime timestamp)
        {
            return Task.FromResult(2);
        }
    }
```

Creating the WebApi controller
```CS
    public class WebApiApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            ServiceFactory.CreateServiceImplementationForAllInCotainingAssembly<ITestApi>();
            ServiceFactory.FinalizeRegistration();

            this.LoadBindingConfiguration<TestBlueprint>();
           
            AreaRegistration.RegisterAllAreas();
            GlobalConfiguration.Configure(WebApiConfig.Register);
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
        }
    }
```
Adding swagger to the service add the following line in Swashbuckle's SwaggerConfig.cs (added with the nuget)
Add the nuget: Install-Package Stardust.Interstellar.Swashbuckle

```CS
    c.ConfigureStardust();
```
Creating client extensions
```CS
    [AttributeUsage(AttributeTargets.Method|AttributeTargets.Interface)]
    public sealed class CallingMachineNameAttribute:HeaderInspectorAttributeBase
    {
        public override IHeaderHandler[] GetHandlers()
        {
            return new IHeaderHandler[] {new CallingMachineNameHandler()};
        }
    }

    public class CallingMachineNameHandler : IHeaderHandler
    {
        public void SetHeader(HttpWebRequest req)
        {
            req.Headers.Add("x-callingMachine",Environment.MachineName);
        }
    }
```
Statefull extesions can be created like this:
```CS
[AttributeUsage(AttributeTargets.Interface | AttributeTargets.Method)]
    public class PerformanceHeadersAttribute : HeaderInspectorAttributeBase
    {
        public override IHeaderHandler[] GetHandlers()
        {
            return new IHeaderHandler[] { new PerformanceHeadersHandler() };
        }
    }



    internal class PerformanceHeadersHandler : StatefullHeaderHandlerBase
    {
        private const string StardustTimerKey = "x-stardusttimer";

        protected override void DoSetHeader(StateDictionary state, HttpWebRequest req)
        {
            state.SetState(StardustTimerKey, Stopwatch.StartNew());//add to state container
        }

        protected override void DoGetHeader(StateDictionary state, HttpWebResponse response)
        {
            var sw = state.GetState<Stopwatch>(StardustTimerKey);//get from state container
            sw.Stop();
            var server = response.Headers[StardustTimerKey];
            if (!string.IsNullOrWhiteSpace(server))
            {
                var serverTime = long.Parse(server);
                var latency = sw.ElapsedMilliseconds - serverTime;
                state.Extras.Add("latency",latency);//add to client output
                state.Extras.Add("serverTime",serverTime);
                state.Extras.Add("totalTime",sw.ElapsedMilliseconds);
            }

        }

        protected override void DoSetServiceHeaders(StateDictionary state, HttpResponseHeaders headers)
        {
            var sw =  state.GetState<Stopwatch>(StardustTimerKey);
            sw.Stop();
            headers.Add(StardustTimerKey, sw.ElapsedMilliseconds.ToString());
        }

        protected override void DoGetServiceHeader(StateDictionary state, HttpRequestHeaders headers)
        {
            state.Add(StardustTimerKey, Stopwatch.StartNew());
        }
    }
```

To wrap existing wcf services as WebApi services:
```CS
protected void Application_Start()
        {
            WcfServiceProvider.RegisterWcfAdapters();   //Adds the wcf adapter package to the generator
            ServiceFactory.CreateServiceImplementationForAllInCotainingAssembly<ITestApi>(); //Same as before
            ServiceFactory.FinalizeRegistration();

            this.LoadBindingConfiguration<TestBlueprint>();
           
            AreaRegistration.RegisterAllAreas();
            GlobalConfiguration.Configure(WebApiConfig.Register);
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
        }
``` 

## Resilience 

Retry:

> Enable an application to handle anticipated, temporary failures when it attempts to connect to a service or network resource by transparently retrying an operation that has previously failed in the expectation that the cause of the failure is transient. This pattern can improve the stability of the application.
>> [Retry pattern ms p&p](https://msdn.microsoft.com/en-us/library/dn589788.aspx)

The client now supports automatic retry on transient failures, like network or connectivity issues.
Add the RetryAttribute to the interface or method that should be retryable. Setting the incremetalWait flags will increase the delay between retries.
In the sample below the action ApplyAsync will retry 3 times with the intervals (1sec,2sec,6sec)

```CS
    [IRoutePrefix("api")]
    [CallingMachineName]
    [PerformanceHeaders]
    [ErrorHandler(typeof(TestHandler))]
    public interface ITestApi
    {
        [Route("test5/{id}")]
        [HttpGet]
        [Retry(3,1000,true)]//Tells the framework to retry transient errors 3 times with a base interval of 1 sec (1000ms) and multiply with the retry count
        Task<StringWrapper> ApplyAsync([In(InclutionTypes.Path)] string id, [In(InclutionTypes.Path)]string name, [In(InclutionTypes.Path)]string item3, [In(InclutionTypes.Path)]string item4);
    }
```

Circuit Breaker:

> The basic idea behind the circuit breaker is very simple. You wrap a protected function call in a circuit breaker object, 
> which monitors for failures. Once the failures reach a certain threshold, the circuit breaker trips, and all further calls to the circuit breaker return with an error, without the protected call being made at all.
>> [Martin Fowler](http://martinfowler.com/bliki/CircuitBreaker.html)

The CircuitBreakerAttribute is applied to the service interface with information regarding threshold, timeouts and ignored exceptions and HttpStatusCodes.
[CircuitBreaker(50, 1)] tells the breaker to allow 50 errors before the circuit breaker trips, it will go to half open state after 1 minute. If there are no errors within 1 minute the failure counter is reset.

Not all errors from an api is catastrophic errors. Some errors are a natural part of the api flow of control and should not cause the circuit breaker to trip.

```CS
    [IRoutePrefix("api")]
    [CallingMachineName]
    [PerformanceHeaders]
    [ErrorHandler(typeof(TestHandler))]
    [CircuitBreaker(50, 1)]//includes the service in a circuit breaker, with a threshold of 50 errors, and a timeout of 1 minute
    public interface ITestApi
    {
        [Route("test/{id}")]
        [HttpGet]
        string Apply1([In(InclutionTypes.Path)] string id, [In(InclutionTypes.Path)]string name);

        [Route("test2/{id}")]
        [HttpGet]
        [UseXml]
        Task<StringWrapper> Apply2([In(InclutionTypes.Path)] string id, [In(InclutionTypes.Path)]string name, [In(InclutionTypes.Header)]string item3);

        [Route("test3/{id}")]
        [HttpGet]
        [AuthorizeWrapper(null)]
        string Apply3([In(InclutionTypes.Path)] string id, [In(InclutionTypes.Path)]string name, [In(InclutionTypes.Header)]string item3, [In(InclutionTypes.Header)]string item4);

        [Route("put1/{id}")]
        [HttpPut]
        void Put([In(InclutionTypes.Path)] string id, [In(InclutionTypes.Body)] DateTime timestamp);

        
        [Route("test5/{id}")]
        [HttpGet]
        [Retry(10,3,false)]
        Task<StringWrapper> ApplyAsync([In(InclutionTypes.Path)] string id, [In(InclutionTypes.Path)]string name, [In(InclutionTypes.Path)]string item3, [In(InclutionTypes.Path)]string item4);

        [Route("put2/{id}")]
        [HttpPut]
        [ServiceDescription("Sample description", Responses = "404;not found|401;Unauthorized access")]
        Task PutAsync([In(InclutionTypes.Path)] string id, [In(InclutionTypes.Body)] DateTime timestamp);

        [Route("failure/{id}")]
        [HttpPut]
        Task FailingAction([In(InclutionTypes.Path)] string id, [In(InclutionTypes.Body)] string timestamp);

        [Route("opt")]
        [HttpOptions]
        Task<List<string>> GetOptions();

        [Route("head")]
        [HttpHead]
        Task GetHead();
    }

```
## Graph client [experimental]
Creating a graph api and client

The graph context:
```CS
public class GraphTestApi:GraphContext<Employee>
    {
        public GraphTestApi(string baseUrl) : base(baseUrl)
        {
            Id = "grabUserIdFromIdentity";
        }
        public IGraphCollection<Employee> Employees
        {
            get
            {
                return CreateGraphCollection<Employee>();
            }
        }

        public IGraphItem<Employee> Me
        {
            get
            {
                return CreateGraphItem<Employee>(Id);
            }
        }
    }
```
and the Employee data type:

```CS
public class Employee : GraphBase
    {
        private string name;

        public string Name
        {
            get
            {
                return name;
            }
            set
            {
                name = value;
            }
        }

        public string Email { get; set; }

        [JsonIgnore]
        public Employee Manager
        {
            get
            {
                return CreateGraphItem<Employee>(ManagerId).Value;
            }
        }

        public async Task<Employee> GetManagerAsync()
        {
            return await CreateGraphItem<Employee>(ManagerId).GetAsync();
        }

        [JsonIgnore]
        public IGraphCollection<Employee> Colleagues
        {
            get
            {
                
                return CreateGraphCollection<Employee>("colleagues", name);
            }
        }

        public string ManagerId { get; set; }
    }
```

To add support for WCF rest annotations:
Install the nuget: Install-Package Stardust.Interstellar.Rest.Legacy

and call the wcf addon before creating the controllers:
```CS
 public class WebApiApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            WcfServiceProvider.RegisterWcfAdapters();//adds the wcf addon
            ServiceFactory.CreateServiceImplementationForAllInCotainingAssembly<ITestApi>();//generates the controllers
            ServiceFactory.FinalizeRegistration(); /registers the new controllers with mvc webapi

            this.LoadBindingConfiguration<TestBlueprint>();
           
            AreaRegistration.RegisterAllAreas();
            GlobalConfiguration.Configure(WebApiConfig.Register);
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
        }
    }

```

In some cases you might want to allow the request body to be dynamic and extendable. By adding IServiceWithGlobalParameters to the service interface the framework will inject properties, at the root level of the json document, defined during app startup.

The global properties are tied to the service so there are two ways of doing this:
from the instance
```CS

var myServiceInstance=ProxyFactory.CreateInstance<IMyExtendableService>(serviceRoot);
myServiceInstance.SetGlobalProperty("myCustomGlobalProperty1", DateTime.UtcNow);//injects the utc datetime of application start (the time the global prrperty is defined)
myServiceInstance.SetGlobalProperty("myCustomGlobalProperty2", new ScopedValueFetcher(() => new { timestamp=DateTime.UtcNow, created=DateTime.UtcNow}); //injects the datetime of service invocation. The ScopedValueFetcher can access any of the context/thread variables avaiable.


```
or
```CS

GlobalParameterExtensions.SetGlobalProperty<IMyExtendableService>("myCustomGlobalProperty1", DateTime.UtcNow);//injects the utc datetime of application start (the time the global prrperty is defined)
GlobalParameterExtensions.SetGlobalProperty<IMyExtendableService>("myCustomGlobalProperty2", new ScopedValueFetcher(() => new { timestamp=DateTime.UtcNow, created=DateTime.UtcNow}); //injects the datetime of service invocation. The ScopedValueFetcher can access any of the context/thread variables avaiable.


```

See [Stardust.KeenIo.Client](https://github.com/JonasSyrstad/Stardust.KeenIo.Client "Stardust.KeenIo.Client") for a demo/sample project on building .net client api's for existing rest services. It currenly supports Adding events and getting collection info from the api.
