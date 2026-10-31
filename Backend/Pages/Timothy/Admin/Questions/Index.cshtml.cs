using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SkillBridge.Web.Features.Quizzes;
namespace SkillBridge.Web.Pages.Admin.Questions;

public sealed class IndexModel(QuestionStore store, QuizAttemptStore attempts) : PageModel
{
    public QuestionQuiz Quiz { get; private set; } = null!;
    public List<QuizQuestion> Questions { get; private set; } = [];
    [TempData] public string? StatusMessage { get; set; }
    public async Task<IActionResult> OnGetAsync(long quizId, CancellationToken cancellationToken)
    {
        var quiz = await store.QuizAsync(quizId, cancellationToken);
        if (quiz is null) return NotFound();
        Quiz = quiz;
        Questions = await attempts.QuestionsAsync(quizId, cancellationToken);
        return Page();
    }
    public async Task<IActionResult> OnPostDeleteAsync(long quizId, long id, CancellationToken cancellationToken)
    {
        try { await store.DeleteAsync(quizId, id, cancellationToken); StatusMessage = "Question deleted."; }
        catch (ValidationException exception) { StatusMessage = exception.Message; }
        return RedirectToPage(new { quizId });
    }
}
