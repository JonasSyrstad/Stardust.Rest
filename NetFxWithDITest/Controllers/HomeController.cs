using NetFxWithDITest.Apis;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Web.Mvc;
using Stardust.Continuum.Client;

namespace NetFxWithDITest.Controllers
{
    public class HomeController : Controller
    {
        private readonly IServiceProvider _provider;
        private readonly IDummyService2 _service;
        private readonly ILogStream _ls;

        public HomeController(IServiceProvider provider, IDummyService2 service, ILogStream ls)
        {
            _provider = provider;
            _service = service;
            _ls = ls;
        }
        public async Task<ActionResult> Index()
        {
            try
            {
                await _ls.AddStream("test", "test", new StreamItem
                {
                    Message = "test"
                });
            }
            catch (Exception ex)
            {
                Debug.Print(ex.Message);
            }
            ViewBag.Title = "Home Page " + _provider.GetType().FullName;
            ViewBag.ServiceInstance = _service.GetType().FullName;
            try
            {
                var result = _service.Test("TestMessage");
                ViewBag.ServiceInstance += $". Response: {result}";
            }
            catch (Exception e)
            {
                ViewBag.ServiceInstance += $". Error: {e.Message}";
            }
            return View();
        }
    }
}
