using Microsoft.AspNetCore.Mvc.RazorPages;

namespace SkillBridge.Web.Pages;

public sealed class StatusModel : PageModel
{
    public int ResponseStatusCode { get; private set; }
    public string Heading => ResponseStatusCode == 404 ? "We could not find that page" : "We could not complete that request";

    public void OnGet(int code)
    {
        ResponseStatusCode = code is >= 400 and <= 599 ? code : 404;
        Response.StatusCode = ResponseStatusCode;
    }
}
