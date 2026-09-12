using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SkillBridge.Web.Features.Quizzes;

namespace SkillBridge.Web.Pages.Quizzes;

[Authorize]
public sealed class StartModel(IQuizReader quizzes) : PageModel
{
    public QuizOverview Quiz { get; private set; } = null!;

    public async Task<IActionResult> OnGetAsync(long quizId, CancellationToken cancellationToken)
    {
        if (quizId <= 0) return NotFound();
        if (!long.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)) return Challenge();

        var quiz = await quizzes.GetOverviewAsync(quizId, userId, User.IsInRole("Admin"), cancellationToken);
        if (quiz is null) return NotFound();

        Quiz = quiz;
        return Page();
    }
}
