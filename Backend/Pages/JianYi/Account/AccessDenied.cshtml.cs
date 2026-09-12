using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace SkillBridge.Web.Pages.Account;

[AllowAnonymous]
public sealed class AccessDeniedModel : PageModel
{
    public void OnGet() => Response.StatusCode = StatusCodes.Status403Forbidden;
}
