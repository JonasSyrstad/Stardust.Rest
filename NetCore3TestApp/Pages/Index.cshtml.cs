using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
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
            var d = await _client.Params("test123",
                new Dictionary<string, string> { { "$filter", "some filter" }, { "$top", "100" } });
            
        }
    }
}
