using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SkillBridge.Web.Features.Quizzes;
using SkillBridge.Web.Infrastructure.Data;
using MySqlConnector;

namespace SkillBridge.Web.Pages.Admin.Quizzes;

[Authorize(Roles = "Admin")]
public sealed class CreateModel(IQuizReader quizzes, MySqlConnectionFactory connections) : PageModel
{
//from ccourses somewhere.
    public IReadOnlyList<QuizSummary> Quizzes { get; private set; } = [];

public string? ErrorMessage { get; set; }
    [BindProperty, Required, StringLength(120, MinimumLength = 3)] public string Title { get; set; } = "";
    [BindProperty, Range(1, long.MaxValue)] public long CourseId { get; set; }
    [BindProperty, Required, StringLength(2000)] public string instructions { get; set; } = "";
    [BindProperty, Range(1,100)] public decimal pass_percentage { get; set; } = 60;
    [BindProperty] public bool IsPublished { get; set; }
    public List<(long Id, string Title)> Courses { get; private set; } = [];
    private async Task LoadCoursesAsync(CancellationToken cancellationToken)
    {
        await using var connection = await connections.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT c.id,c.title FROM courses c WHERE NOT EXISTS (SELECT 1 FROM quizzes q WHERE q.course_id=c.id) ORDER BY c.title;";
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        Courses = [];
        while (await reader.ReadAsync(cancellationToken)) Courses.Add((reader.GetInt64("id"),reader.GetString("title")));
    }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Quizzes = await quizzes.GetAllAsync(cancellationToken);
            await LoadCoursesAsync(cancellationToken);
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        Title = (Title ?? "").Trim();
        instructions = (instructions ?? "").Trim();
        await LoadCoursesAsync(cancellationToken);
        if (!Courses.Any(course => course.Id == CourseId)) ModelState.AddModelError(nameof(CourseId), "Choose a course without a quiz.");
        if (Title.Length < 3) ModelState.AddModelError(nameof(Title), "Use at least 3 characters.");
        if (!Title.Any(char.IsLetter)) ModelState.AddModelError(nameof(Title), "Include words in the quiz title.");
        if (!instructions.Any(char.IsLetter)) ModelState.AddModelError(nameof(instructions), "Describe how to complete the quiz using words.");
        if (IsPublished) ModelState.AddModelError(nameof(IsPublished), "Create a draft first, then add valid questions before publishing.");
        if (!ModelState.IsValid)
        {
            Quizzes = await quizzes.GetAllAsync(cancellationToken);
            await LoadCoursesAsync(cancellationToken);
            return Page();
        }

        await using var connection = await connections.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            string sql = "INSERT INTO quizzes (course_id, title, instructions, pass_percentage, is_published) VALUES (@courseid, @title, @instructions, @pass_percentage, @publish);";

            using (var command = new MySqlCommand(sql, connection, (MySqlTransaction)transaction))
            {
                //match with bind properties otherwise error
                command.Parameters.AddWithValue("@courseid", CourseId);
                command.Parameters.AddWithValue("@title", Title);
                command.Parameters.AddWithValue("@instructions", instructions);
                command.Parameters.AddWithValue("@pass_percentage", pass_percentage);
                command.Parameters.AddWithValue("@publish", IsPublished);
                await command.ExecuteNonQueryAsync(cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
            return RedirectToPage("/Timothy/Admin/Quizzes/Index");
     }
        catch (MySqlException exception) when (exception.Number is 1062 or 1452 or 3819 or 4025)
        {
            await transaction.RollbackAsync(cancellationToken);
            ModelState.AddModelError(string.Empty, "Check the field limits and select a course without an existing quiz.");
            Quizzes = await quizzes.GetAllAsync(cancellationToken);
            await LoadCoursesAsync(cancellationToken);
            return Page();
        }
    }


}
