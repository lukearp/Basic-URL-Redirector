using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Primitives;
using System.Web;

namespace Basic_URL_Redirector.Pages;

public class IndexModel : PageModel
{
    private readonly ILogger<IndexModel> _logger;
    public string Url {get;set;}

    public IndexModel(ILogger<IndexModel> logger)
    {
       
    }

    public void OnGet()
    {
        Request.Headers.TryGetValue("X-Forwarded-Host", out StringValues hostname);
        Url = HttpContext.Request.Path;
    }
}
