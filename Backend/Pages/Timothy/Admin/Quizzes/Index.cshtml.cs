using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SkillBridge.Web.Features.Quizzes;

namespace SkillBridge.Web.Pages.Admin.Quizzes;

[Authorize(Roles = "Admin")]
public sealed class IndexModel(IQuizReader quizzes) : PageModel
{
    public IReadOnlyList<QuizSummary> Quizzes { get; private set; } = [];

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Quizzes = await quizzes.GetAllAsync(cancellationToken);
    }
}
