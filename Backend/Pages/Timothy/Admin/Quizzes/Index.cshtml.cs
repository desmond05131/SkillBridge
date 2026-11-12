using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using SkillBridge.Web.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SkillBridge.Web.Features.Quizzes;

namespace SkillBridge.Web.Pages.Admin.Quizzes;

[Authorize(Roles = "Admin")]
public sealed class IndexModel(IQuizReader quizzes, MySqlConnectionFactory connections) : PageModel
{
    public IReadOnlyList<QuizSummary> Quizzes { get; private set; } = [];
    [BindProperty(SupportsGet = true), System.ComponentModel.DataAnnotations.StringLength(120)]
    public string? Search { get; set; }
    [BindProperty(SupportsGet = true), System.ComponentModel.DataAnnotations.RegularExpression("^(Draft|Published)$", ErrorMessage = "Choose a valid publication status.")]
    public string? Status { get; set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return;
        Quizzes = await quizzes.GetAllAsync(cancellationToken, Search, Status);
    }
    //
    [TempData] public string? StatusMessage { get; set; }
    public async Task<IActionResult> OnPostDeleteAsync(long quizId, CancellationToken cancellationToken)
    {
        if (quizId <= 0) return NotFound();
        await using var connection = await connections.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "SELECT id FROM quizzes WHERE id=@id FOR UPDATE;";
        command.Parameters.AddWithValue("@id", quizId);
        if (await command.ExecuteScalarAsync(cancellationToken) is null)
        {
            StatusMessage = "Quiz not found.";
            return RedirectToPage();
        }
        command.CommandText = "SELECT COUNT(*) FROM attempts WHERE quiz_id=@id;";
        if (Convert.ToInt64(await command.ExecuteScalarAsync(cancellationToken)) > 0)
        {
            StatusMessage = "This quiz cannot be deleted because learners have started attempts. Their results must be preserved.";
            return RedirectToPage();
        }
        try
        {
            command.CommandText = """
                DELETE o FROM options o JOIN questions q ON q.id=o.question_id WHERE q.quiz_id=@id;
                DELETE FROM questions WHERE quiz_id=@id;
                DELETE FROM quizzes WHERE id=@id;
                """;
            await command.ExecuteNonQueryAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            StatusMessage = "Quiz and its questions deleted.";
        }
        catch (MySqlException exception) when (exception.Number == 1451)
        {
            await transaction.RollbackAsync(cancellationToken);
            StatusMessage = "This quiz cannot be deleted because learner records are linked to it. Nothing was deleted.";
        }
        return RedirectToPage();
    }
}
// here will be form to edit and del
