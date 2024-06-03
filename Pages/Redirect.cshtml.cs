using Microsoft.AspNetCore.Mvc.RazorPages;
using Newtonsoft.Json.Linq;
using Basic_URL_Redirector.Data;

namespace Basic_URL_Redirector.Pages;

public class RedirectModel : PageModel
{
    private readonly ILogger<RedirectModel> _logger;
    public string Url { get; set; }
    public string? hostName { get; set; }
    public string? referer {get;set;}
    public JObject redirectMap { get; set; }

    public RedirectModel(ILogger<RedirectModel> logger)
    {
        _logger = logger;
    }

    public async Task OnGet()
    {
        Url = Request.QueryString.ToString().Split("?originalPath=")[1];
        hostName = HttpContext.Request.Headers["X-Forwarded-Host"];
        referer = HttpContext.Request.Headers["Referer"];
        /*if(Url.EndsWith(".pdf"))
        {
            Response.Redirect("https://www.google.com");
        }*/
        if (hostName != null)
        {
            if(referer == null)
            {
                referer = "No Referer Header";
            }
            string target = DataLoad.GetRedirect(hostName, Url);
            _logger.LogInformation("Original Target: {Original}, New Target: {New}, Timestamp: {Time}, Referer: {Referer}", [(hostName + Url),target,DateTime.Now.ToString(),referer]);
            Response.Redirect(target);
        }
    }
}
