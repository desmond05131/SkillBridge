using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SkillBridge.Web.Features.Courses;

namespace SkillBridge.Web.Pages.Courses;

public sealed class IndexModel(ICourseReader courses) : PageModel
{
    [BindProperty(SupportsGet = true)]
    [StringLength(100)]
    public string? Search { get; set; }

    public IReadOnlyList<CourseSummary> Courses { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return Page();

        Courses = await courses.GetPublishedAsync(Search, cancellationToken);
        return Page();
    }
}
