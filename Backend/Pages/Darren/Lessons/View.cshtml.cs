using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SkillBridge.Web.Features.Lessons;

namespace SkillBridge.Web.Pages.Lessons;

[Authorize]
public sealed class ViewModel(ILessonReader lessons) : PageModel
{
    public LessonDetail Detail { get; private set; } = null!;

    public async Task<IActionResult> OnGetAsync(long id, CancellationToken cancellationToken)
    {
        if (id <= 0) return NotFound();
        if (!long.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)) return Challenge();

        var lesson = await lessons.GetByIdAsync(id, userId, User.IsInRole("Admin"), cancellationToken);
        if (lesson is null) return NotFound();

        Detail = lesson;
        return Page();
    }
}
