using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MySqlConnector;
using SkillBridge.Web.Features.Quizzes;
namespace SkillBridge.Web.Pages.Admin.Questions;

public sealed class EditModel(QuestionStore store, QuizAttemptStore attempts) : PageModel
{
    [BindProperty] public QuestionInput Input { get; set; } = new();
    public QuestionQuiz Quiz { get; private set; } = null!;
    public async Task<IActionResult> OnGetAsync(long quizId, long? id, CancellationToken cancellationToken)
    {
        var quiz = await store.QuizAsync(quizId, cancellationToken);
        if (quiz is null) return NotFound();
        Quiz = quiz;
        var questions = await attempts.QuestionsAsync(quizId, cancellationToken);
        Input.QuizId = quizId;
        Input.SequenceNumber = questions.Count == 0 ? 1 : questions.Max(question => question.SequenceNumber) + 1;
        if (id is null) return Page();
        var question = questions.SingleOrDefault(question => question.Id == id);
        if (question is null) return NotFound();
        Input = new()
        {
            Id = question.Id,
            QuizId = quizId,
            Text = question.Text,
            Explanation = question.Explanation,
            Marks = question.Marks,
            SequenceNumber = question.SequenceNumber,
            Options = question.Options.Select(option => option.Text).ToArray(),
            CorrectOption = question.Options.FindIndex(option => option.IsCorrect)
        };
        return Page();
    }
    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        var quiz = await store.QuizAsync(Input.QuizId, cancellationToken);
        if (quiz is null) return NotFound();
        Quiz = quiz;
        if (string.IsNullOrWhiteSpace(Input.Text) || Input.Text.Trim().Length < 5) ModelState.AddModelError("Input.Text", "Use at least 5 characters.");
        if (string.IsNullOrWhiteSpace(Input.Explanation)) ModelState.AddModelError("Input.Explanation", "Add a helpful explanation.");
        if (Input.Options is null || Input.Options.Length != 4 || Input.Options.Any(option => string.IsNullOrWhiteSpace(option) || option.Trim().Length > 500))
            ModelState.AddModelError("Input.Options", "Provide exactly four non-empty options, each up to 500 characters.");
        else if (Input.Options.Select(option => option.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).Count() != 4)
            ModelState.AddModelError("Input.Options", "The four options must be different.");
        if (!ModelState.IsValid) return Page();
        try { await store.SaveAsync(Input, cancellationToken); }
        catch (ValidationException exception) { ModelState.AddModelError(string.Empty, exception.Message); return Page(); }
        catch (MySqlException exception) when (exception.Number == 1062)
        { ModelState.AddModelError("Input.SequenceNumber", "Choose an unused question order number."); return Page(); }
        TempData["StatusMessage"] = "Question saved.";
        return RedirectToPage("Index", new { quizId = Input.QuizId });
    }
}
