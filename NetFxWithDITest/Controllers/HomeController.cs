using NetFxWithDITest.Apis;
using System;
using System.Web.Mvc;

namespace NetFxWithDITest.Controllers
{
    public class HomeController : Controller
    {
        private readonly IServiceProvider _provider;
        private readonly IDummyService _service;

        public HomeController(IServiceProvider provider, IDummyService service)
        {
            _provider = provider;
            _service = service;
        }
        public ActionResult Index()
        {
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
