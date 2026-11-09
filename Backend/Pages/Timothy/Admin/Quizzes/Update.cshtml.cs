using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SkillBridge.Web.Features.Quizzes;
using SkillBridge.Web.Infrastructure.Data;
using MySqlConnector;

namespace SkillBridge.Web.Pages.Admin.Quizzes;

[Authorize(Roles = "Admin")]
public sealed class UpdateModel(MySqlConnectionFactory connections, QuizAttemptStore attempts) : PageModel
{

   [BindProperty]
    public long QuizId { get; set; }

    [BindProperty]
    public long CourseId { get; set; } // Display-only or tracked for reference

    [BindProperty, Required, StringLength(120, MinimumLength = 3)]
    public string Title { get; set; } = string.Empty;

    [BindProperty, Required, StringLength(2000)]
    public string Instructions { get; set; } = string.Empty;

    [BindProperty, Range(1,100)]
    public decimal PassPercentage { get; set; }

    [BindProperty]
    public bool IsPublished { get; set; }

    [TempData]
    public string? SuccessMessage { get; set; }
    public string? ErrorMessage { get; set; }

    public async Task<IActionResult> OnGetAsync(long id, CancellationToken cancellationToken)
    {
        if (!User.IsInRole("Admin")) return Challenge();

        await LoadQuizDataAsync(id, cancellationToken);
        if (QuizId == 0) return NotFound();

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (!User.IsInRole("Admin")) return Challenge();

        Title = (Title ?? "").Trim();
        Instructions = (Instructions ?? "").Trim();
        if (Title.Length < 3) ModelState.AddModelError(nameof(Title), "Use at least 3 characters.");
        if (!Title.Any(char.IsLetter)) ModelState.AddModelError(nameof(Title), "Include words in the quiz title.");
        if (!Instructions.Any(char.IsLetter)) ModelState.AddModelError(nameof(Instructions), "Describe how to complete the quiz using words.");
        await using (var connection = await connections.OpenAsync(cancellationToken))
        {
            await using var command = new MySqlCommand("SELECT course_id FROM quizzes WHERE id=@id;", connection);
            command.Parameters.AddWithValue("@id", QuizId);
            var courseId = await command.ExecuteScalarAsync(cancellationToken);
            if (courseId is null) return NotFound();
            CourseId = Convert.ToInt64(courseId);
        }
        if (IsPublished && !QuizAttemptStore.IsReady(await attempts.QuestionsAsync(QuizId, cancellationToken)))
            ModelState.AddModelError(nameof(IsPublished), "Add questions with four options and one correct answer each before publishing.");
        if (!ModelState.IsValid) return Page();

        try
        {
            await using var conn = await connections.OpenAsync(cancellationToken);

            // Update everything EXCEPT quiz id and course id
            string sql = """
                UPDATE quizzes
                SET title = @title,
                    instructions = @instructions,
                    pass_percentage = @passPercentage,
                    is_published = @isPublished
                WHERE id = @quizId AND NOT EXISTS (SELECT 1 FROM attempts WHERE quiz_id=@quizId);
                """;

            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@title", Title.Trim());
            cmd.Parameters.AddWithValue("@instructions", Instructions.Trim());
            cmd.Parameters.AddWithValue("@passPercentage", PassPercentage);
            cmd.Parameters.AddWithValue("@isPublished", IsPublished);
            cmd.Parameters.AddWithValue("@quizId", QuizId);

            if (await cmd.ExecuteNonQueryAsync(cancellationToken) == 0)
            {
                ErrorMessage = "This quiz does not exist or has attempts. Attempted quizzes cannot be edited.";
                return Page();
            }

            SuccessMessage = "Quiz updated successfully.";
            return RedirectToPage("/Timothy/Admin/Quizzes/Index");
        }
        catch (MySqlException exception) when (exception.Number is 1452 or 3819 or 4025)
        {
            ErrorMessage = "Failed to update quiz. Please try again.";
            return Page();
        }
    }

    private async Task LoadQuizDataAsync(long id, CancellationToken cancellationToken)
    {
            await using var conn = await connections.OpenAsync(cancellationToken);

        string sql = "SELECT id, course_id, title, instructions, pass_percentage, is_published FROM quizzes WHERE id = @id;";
        await using var cmd = new MySqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@id", id);

        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        if (await reader.ReadAsync(cancellationToken))
        {
            QuizId = reader.GetInt64("id");
            CourseId = reader.GetInt64("course_id");
            Title = reader.GetString("title");
            Instructions = reader.GetString("instructions");
            PassPercentage = reader.GetDecimal("pass_percentage");
            IsPublished = reader.GetBoolean("is_published");
        }
        // return RedirectToPage("/Timothy/Admin/Quizzes/Index");
    }

}
