using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SkillBridge.Web.Features.Quizzes;

namespace SkillBridge.Web.Pages.Quizzes;

[Authorize]
public sealed class StartModel(IQuizReader quizzes, QuizAttemptStore attempts) : PageModel
{
    public QuizOverview Quiz { get; private set; } = null!;
    public List<AttemptHistory> PreviousAttempts { get; private set; } = [];
    public AttemptHistory? LastAttempt => PreviousAttempts.FirstOrDefault();
    public int TotalMarks { get; private set; }
    public bool IsReady { get; private set; }

    public async Task<IActionResult> OnGetAsync(long quizId, CancellationToken cancellationToken)
    {
        if (quizId <= 0) return NotFound();
        if (!long.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)) return Challenge();

        var quiz = await quizzes.GetOverviewAsync(quizId, userId, User.IsInRole("Admin"), cancellationToken);
        if (quiz is null) return NotFound();

        Quiz = quiz;
        var questions = await attempts.QuestionsAsync(quizId, cancellationToken);
        IsReady = QuizAttemptStore.IsReady(questions);
        TotalMarks = questions.Sum(question => question.Marks);
        PreviousAttempts = await attempts.HistoryAsync(quizId, userId, cancellationToken);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(long quizId, CancellationToken cancellationToken)
    {
        var result = await OnGetAsync(quizId, cancellationToken);
        if (result is not PageResult) return result;
        if (!IsReady) return Page();
        var userId = long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        try
        {
            var attemptId = await attempts.StartAsync(quizId, userId, cancellationToken);
            return RedirectToPage("/Timothy/Quizzes/Quiz", new { attemptId });
        }
        catch (System.ComponentModel.DataAnnotations.ValidationException exception)
        {
            ModelState.AddModelError(string.Empty, exception.Message);
            return Page();
        }
    }
}
