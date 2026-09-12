using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SkillBridge.Web.Features.Lessons;

namespace SkillBridge.Web.Pages.Admin.Lessons;

[Authorize(Roles = "Admin")]
public sealed class IndexModel(ILessonReader lessons) : PageModel
{
    public IReadOnlyList<LessonSummary> Lessons { get; private set; } = [];

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Lessons = await lessons.GetForAdministrationAsync(cancellationToken);
    }
}
