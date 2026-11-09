using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SkillBridge.Web.Features.Lessons;
namespace SkillBridge.Backend.Pages.Darren.Admin.Resources;

public class IndexModel(ResourceStore resources) : PageModel
{
    public List<ResourceFile> Resources { get; private set; } = [];
    [TempData] public string? StatusMessage { get; set; }
    [BindProperty(SupportsGet = true), System.ComponentModel.DataAnnotations.StringLength(120)]
    public string? Search { get; set; }
    [BindProperty(SupportsGet = true), System.ComponentModel.DataAnnotations.RegularExpression("^(Pdf|Video|Audio)$", ErrorMessage = "Choose a valid resource type.")]
    public string? Type { get; set; }
    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return;
        Resources = await resources.ListAsync(null, cancellationToken, Search, Type);
    }
    public async Task<IActionResult> OnPostDeleteAsync(long id, CancellationToken cancellationToken)
    {
        await resources.DeleteAsync(id, cancellationToken);
        StatusMessage = "Resource deleted.";
        return RedirectToPage();
    }
}
