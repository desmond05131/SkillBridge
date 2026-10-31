using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SkillBridge.Web.Features.Lessons;
namespace SkillBridge.Backend.Pages.Darren.Admin.Resources;

public class IndexModel(ResourceStore resources) : PageModel
{
    public List<ResourceFile> Resources { get; private set; } = [];
    [TempData] public string? StatusMessage { get; set; }
    public async Task OnGetAsync(CancellationToken cancellationToken) => Resources = await resources.ListAsync(null, cancellationToken);
    public async Task<IActionResult> OnPostDeleteAsync(long id, CancellationToken cancellationToken)
    {
        await resources.DeleteAsync(id, cancellationToken);
        StatusMessage = "Resource deleted.";
        return RedirectToPage();
    }
}
