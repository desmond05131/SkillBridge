
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SkillBridge.Web.Features.Courses;
using SkillBridge.Web.Infrastructure.Data;

namespace SkillBridge.Web.Pages.Courses;

[Authorize(Roles = "Member")]
public sealed class MineModel(
    ICourseReader courses,
    MySqlConnectionFactory database) : PageModel
{
    public record LearningState(int CompletedLessons, long? ContinueLessonId, long? QuizId);
    public Dictionary<long, LearningState> Learning { get; } = [];

    public IReadOnlyList<CourseSummary> Courses { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync(
        CancellationToken cancellationToken)
    {
        if (!long.TryParse(
            User.FindFirstValue(ClaimTypes.NameIdentifier),
            out var userId))
        {
            return Challenge();
        }

        Courses = await courses.GetEnrolledAsync(
            userId, cancellationToken);

        await using var connection = await database.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT c.id,
                (SELECT COUNT(*) FROM lesson_progress p JOIN lessons l ON l.id = p.lesson_id
                 WHERE l.course_id = c.id AND p.user_id = @userId AND p.is_completed = TRUE),
                (SELECT l.id FROM lessons l WHERE l.course_id = c.id
                 ORDER BY EXISTS(SELECT 1 FROM lesson_progress p WHERE p.lesson_id=l.id
                     AND p.user_id=@userId AND p.is_completed=TRUE), l.sequence_number, l.id LIMIT 1),
                (SELECT q.id FROM quizzes q WHERE q.course_id = c.id AND q.is_published = TRUE LIMIT 1)
            FROM courses c JOIN enrolments e ON e.course_id = c.id
            WHERE e.user_id = @userId AND c.is_published = TRUE
            """;
        command.Parameters.AddWithValue("@userId", userId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
            Learning[reader.GetInt64(0)] = new LearningState(reader.GetInt32(1),
                reader.IsDBNull(2) ? null : reader.GetInt64(2), reader.IsDBNull(3) ? null : reader.GetInt64(3));

        return Page();
    }

    public async Task<IActionResult> OnPostRemoveAsync(
        long id,
        CancellationToken cancellationToken)
    {
        if (id <= 0)
        {
            return BadRequest();
        }

        if (!long.TryParse(
            User.FindFirstValue(ClaimTypes.NameIdentifier),
            out var userId))
        {
            return Challenge();
        }

        await using var connection =
            await database.OpenAsync(cancellationToken);

        await using var command =
            connection.CreateCommand();

        command.CommandText = """
            DELETE FROM enrolments
            WHERE user_id = @userId
              AND course_id = @courseId;
            """;

        command.Parameters.AddWithValue("@userId", userId);
        command.Parameters.AddWithValue("@courseId", id);

        await command.ExecuteNonQueryAsync(cancellationToken);

        return RedirectToPage();
    }
}
