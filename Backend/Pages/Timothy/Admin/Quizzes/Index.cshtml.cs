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
        await using var connection = await connections.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM quizzes WHERE id=@id";
        command.Parameters.AddWithValue("@id", quizId);
        try
        {
            var deleted = await command.ExecuteNonQueryAsync(cancellationToken);
            StatusMessage = deleted > 0 ? "Quiz deleted." : "Quiz not found.";
        }
        catch (MySqlException exception) when (exception.Number == 1451)
        {
            StatusMessage = "This quiz has questions or attempts and cannot be deleted.";
        }
        return RedirectToPage();
    }
}
// here will be form to edit and del
