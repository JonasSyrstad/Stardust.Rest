using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Stardust.Interstellar.Rest.Annotations;
using Stardust.Interstellar.Rest.Client;
using Stardust.Interstellar.Rest.Annotations.Messaging;
using Stardust.Interstellar.Rest.Service;

namespace ClientTest
{
    class Program
    {
        static void Main(string[] args)
        {
          var proxy=  ProxyFactory.CreateInstance<ITestApi>("http://localhost/Stardust.Interstellar.Test/");
           var result= proxy.Apply1("test", "test");
            Console.WriteLine(result);
        }
    }
    [IRoutePrefix("api")]
    public interface ITestApi
    {
        [IRoute("test/{id}")]
        [Get]
        string Apply1([In(InclutionTypes.Path)] string id, [In(InclutionTypes.Path)]string name);

        [IRoute("test2/{id}")]
        [Get]
        Task<StringWrapper> Apply2([In(InclutionTypes.Path)] string id, [In(InclutionTypes.Path)]string name, [In(InclutionTypes.Header)]string item3);

        //[Route("test3/{id}")]
        //[HttpGet]
        //[AuthorizeWrapper(null)]
        //string Apply3([In(InclutionTypes.Path)] string id, [In(InclutionTypes.Path)]string name, [In(InclutionTypes.Header)]string item3, [In(InclutionTypes.Header)]string item4);

        //[Route("put1/{id}")]
        //[HttpPut]
        //void Put([In(InclutionTypes.Path)] string id, [In(InclutionTypes.Body)] DateTime timestamp);


        //[Route("test5/{id}")]
        //[HttpGet]
    
        //Task<StringWrapper> ApplyAsync([In(InclutionTypes.Path)] string id, [In(InclutionTypes.Path)]string name, [In(InclutionTypes.Path)]string item3, [In(InclutionTypes.Path)]string item4);

        //[Route("put2/{id}")]
        //[HttpPut]
        //[ServiceDescription("Sample description", Responses = "404;not found|401;Unauthorized access")]
        //Task PutAsync([In(InclutionTypes.Path)] string id, [In(InclutionTypes.Body)] DateTime timestamp);

        //[Route("failure/{id}")]
        //[HttpPut]
        //Task FailingAction([In(InclutionTypes.Path)] string id, [In(InclutionTypes.Body)] string timestamp);

        //[Route("opt")]
        //[HttpOptions]
        //Task<List<string>> GetOptions();

        //[Route("head")]
        //[HttpHead]
        //Task GetHead();


        //[IRoute("tr")]
        //[Get]
        //[Throttling(-1)]
        //Task Throttled();
    }
    public class StringWrapper
    {
        public string Value { get; set; }
        public DateTime? NullDateTime { get; set; }
    }
}
