using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SkillBridge.Web.Features.Courses;

namespace SkillBridge.Web.Pages.Courses;

public sealed class DetailModel(ICourseReader courses) : PageModel
{
    public CourseDetail Detail { get; private set; } = null!;

    public async Task<IActionResult> OnGetAsync(long id, CancellationToken cancellationToken)
    {
        if (id <= 0) return NotFound();

        var detail = await courses.GetByIdAsync(id, User.IsInRole("Admin"), cancellationToken);
        if (detail is null) return NotFound();

        Detail = detail;
        return Page();
    }
}
