using Microsoft.AspNetCore.Mvc.RazorPages;
using NetCore3TestApp.Api;

namespace NetCore3TestApp.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;
        private readonly ITestClient _client;

        public IndexModel(ILogger<IndexModel> logger, ITestClient client)
        {
            _logger = logger;
            _client = client;
        }

        public async Task OnGet()
        {
            var d = await _client.Params("test123", new Dictionary<string, string> { { "$filter", "some filter" }, { "$top", "100" } });
            var x = await _client.MixedParams("123", "123", 10, null, "123-123");
        }
    }
}
