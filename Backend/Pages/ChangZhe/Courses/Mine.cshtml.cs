using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SkillBridge.Web.Features.Courses;

namespace SkillBridge.Web.Pages.Courses;

[Authorize]
public sealed class MineModel(ICourseReader courses) : PageModel
{
    public IReadOnlyList<CourseSummary> Courses { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        if (!long.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)) return Challenge();

        Courses = await courses.GetEnrolledAsync(userId, cancellationToken);
        return Page();
    }
}
