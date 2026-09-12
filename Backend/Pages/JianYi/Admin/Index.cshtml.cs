using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SkillBridge.Web.Features.Administration;

namespace SkillBridge.Web.Pages.Admin;

[Authorize(Roles = "Admin")]
public sealed class IndexModel(IDashboardReader dashboard) : PageModel
{
    public DashboardSummary Summary { get; private set; } = new(0, 0, 0, 0);

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Summary = await dashboard.GetSummaryAsync(cancellationToken);
    }
}
