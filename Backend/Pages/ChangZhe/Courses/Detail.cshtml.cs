
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MySqlConnector;
using SkillBridge.Web.Infrastructure.Data;

namespace SkillBridge.Web.Pages.ChangZhe.Courses;

public class DetailModel(
    MySqlConnectionFactory database) : PageModel
{
    public CourseDetail? Course { get; private set; }

    public List<LessonRow> Lessons { get; } = [];
    public record LessonRow(long Id, string Title, int DurationMinutes, bool IsCompleted);
    public LessonRow? ContinueLesson => Lessons.FirstOrDefault(lesson => !lesson.IsCompleted) ?? Lessons.FirstOrDefault();
    public bool AllLessonsCompleted => Lessons.Count > 0 && Lessons.All(lesson => lesson.IsCompleted);

    public bool HasStarted { get; private set; }

    [TempData]
    public string? CourseMessage { get; set; }

    public record CourseDetail(
        long Id,
        string Title,
        string Category,
        string Summary,
        string Description,
        string Level,
        int DurationMinutes,
        int LessonCount,
        string? ThumbnailUrl,
        bool IsPublished
    );

    private long? GetCurrentUserId()
    {
        if (User.Identity?.IsAuthenticated != true)
            return null;

        var idValue = User.FindFirstValue(ClaimTypes.NameIdentifier);

        return long.TryParse(idValue, out long userId)
            ? userId
            : null;
    }

    public async Task<IActionResult> OnGetAsync(
        long id,
        CancellationToken ct)
    {
        if (id <= 0)
            return NotFound();

        bool isAdmin = User.Identity?.IsAuthenticated == true
            && User.IsInRole("Admin");

        await using var connection = await database.OpenAsync(ct);
        await using var command = connection.CreateCommand();

        command.CommandText = """
            SELECT
                c.id,
                c.title,
                cat.name AS category_name,
                c.summary,
                c.description,
                c.level,
                c.duration_minutes,
                (
                    SELECT COUNT(*)
                    FROM lessons l
                    WHERE l.course_id = c.id
                ) AS lesson_count,
                c.thumbnail_url,
                c.is_published
            FROM courses c
            INNER JOIN categories cat
                ON cat.id = c.category_id
            WHERE c.id = @id
              AND (c.is_published = 1 OR @isAdmin = 1)
            LIMIT 1;
            """;

        command.Parameters.AddWithValue("@id", id);
        command.Parameters.AddWithValue("@isAdmin", isAdmin);

        await using (var reader = await command.ExecuteReaderAsync(ct))
        {
            if (!await reader.ReadAsync(ct))
                return NotFound();

            Course = new CourseDetail(
                Id: reader.GetInt64(0),
                Title: reader.GetString(1),
                Category: reader.GetString(2),
                Summary: reader.IsDBNull(3) ? "" : reader.GetString(3),
                Description: reader.IsDBNull(4) ? "" : reader.GetString(4),
                Level: reader.GetString(5),
                DurationMinutes: reader.GetInt32(6),
                LessonCount: Convert.ToInt32(reader.GetValue(7)),
                ThumbnailUrl: reader.IsDBNull(8) ? null : reader.GetString(8),
                IsPublished: reader.GetBoolean(9)
            );
        }

        await using (var outline = connection.CreateCommand())
        {
            outline.CommandText = """
                SELECT l.id, l.title, l.duration_minutes,
                    EXISTS(SELECT 1 FROM lesson_progress p WHERE p.lesson_id=l.id
                        AND p.user_id=@userId AND p.is_completed=TRUE)
                FROM lessons l WHERE l.course_id=@id ORDER BY l.sequence_number, l.id
                """;
            outline.Parameters.AddWithValue("@userId", GetCurrentUserId());
            outline.Parameters.AddWithValue("@id", id);
            await using var lessons = await outline.ExecuteReaderAsync(ct);
            while (await lessons.ReadAsync(ct))
                Lessons.Add(new LessonRow(lessons.GetInt64(0), lessons.GetString(1), lessons.GetInt32(2), lessons.GetBoolean(3)));
        }

        long? userId = GetCurrentUserId();

        if (Course.IsPublished && userId.HasValue && !isAdmin)
        {
            await using var checkCommand = connection.CreateCommand();

            checkCommand.CommandText = """
                SELECT COUNT(*)
                FROM enrolments
                WHERE user_id = @userId
                  AND course_id = @courseId;
                """;

            checkCommand.Parameters.AddWithValue("@userId", userId.Value);
            checkCommand.Parameters.AddWithValue("@courseId", id);

            var result = await checkCommand.ExecuteScalarAsync(ct);
            HasStarted = Convert.ToInt64(result) > 0;
        }

        return Page();
    }

    public async Task<IActionResult> OnPostStartAsync(
        long id,
        CancellationToken ct)
    {
        if (id <= 0)
            return NotFound();

        if (User.Identity?.IsAuthenticated != true)
            return Challenge();

        if (!User.IsInRole("Member"))
            return Forbid();

        long? userId = GetCurrentUserId();

        if (!userId.HasValue)
        {
            return BadRequest(
                "Unable to identify the signed-in user. Check the login claims.");
        }

        await using var connection = await database.OpenAsync(ct);
        await using var transaction = await connection.BeginTransactionAsync(ct);

        await using (var courseCommand = connection.CreateCommand())
        {
            courseCommand.Transaction = transaction;
            courseCommand.CommandText = """
                SELECT id
                FROM courses
                WHERE id = @id
                  AND is_published = 1 FOR UPDATE;
                """;

            courseCommand.Parameters.AddWithValue("@id", id);

            var result = await courseCommand.ExecuteScalarAsync(ct);

            if (result is null)
                return NotFound();
        }

        await using (var enrolCommand = connection.CreateCommand())
        {
            enrolCommand.Transaction = transaction;
            enrolCommand.CommandText = """
                INSERT INTO enrolments (user_id, course_id)
                VALUES (@userId, @courseId)
                ON DUPLICATE KEY UPDATE user_id = @userId;
                """;

            enrolCommand.Parameters.AddWithValue("@userId", userId.Value);
            enrolCommand.Parameters.AddWithValue("@courseId", id);

            await enrolCommand.ExecuteNonQueryAsync(ct);
        }

        await transaction.CommitAsync(ct);
        var page = await OnGetAsync(id, ct);
        if (page is not PageResult) return page;
        if (ContinueLesson is { } lesson)
            return RedirectToPage("/Darren/Lessons/View", new { id = lesson.Id });

        CourseMessage = "Course saved to My Courses. Lessons are being prepared.";

        return RedirectToPage(
            "/ChangZhe/Courses/Detail",
            new { id });
    }
}
