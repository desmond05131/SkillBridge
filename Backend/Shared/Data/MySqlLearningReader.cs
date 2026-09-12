using MySqlConnector;
using SkillBridge.Web.Features.Administration;
using SkillBridge.Web.Features.Community;
using SkillBridge.Web.Features.Courses;
using SkillBridge.Web.Infrastructure.Data;

namespace SkillBridge.Web.Infrastructure.Content;

public sealed class MySqlLearningReader(MySqlConnectionFactory connectionFactory) : ICourseReader, IForumReader, IDashboardReader
{
    private const string CourseProjection = """
        SELECT c.id, c.title, c.summary, cat.name AS category_name, c.level,
               (SELECT COUNT(*) FROM lessons l WHERE l.course_id = c.id) AS lesson_count,
               c.duration_minutes, c.artwork_key
        FROM courses c
        INNER JOIN categories cat ON cat.id = c.category_id
        """;

    public async Task<IReadOnlyList<CourseSummary>> GetPublishedAsync(string? search, CancellationToken cancellationToken)
    {
        await using var connection = await connectionFactory.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = $"""
            {CourseProjection}
            WHERE c.is_published = TRUE
              AND (@search = '' OR LOCATE(@search, c.title) > 0 OR LOCATE(@search, c.summary) > 0)
            ORDER BY c.title, c.id
            LIMIT 100;
            """;
        command.Parameters.AddWithValue("@search", search?.Trim() ?? string.Empty);
        return await ReadCoursesAsync(command, cancellationToken);
    }

    public async Task<CourseDetail?> GetByIdAsync(long id, bool includeDraft, CancellationToken cancellationToken)
    {
        await using var connection = await connectionFactory.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT c.id, c.title, c.summary, cat.name AS category_name, c.level,
                   (SELECT COUNT(*) FROM lessons l WHERE l.course_id = c.id) AS lesson_count,
                   c.duration_minutes, c.artwork_key, c.description, c.is_published
            FROM courses c
            INNER JOIN categories cat ON cat.id = c.category_id
            WHERE c.id = @id AND (c.is_published = TRUE OR @includeDraft = TRUE);
            """;
        command.Parameters.AddWithValue("@id", id);
        command.Parameters.AddWithValue("@includeDraft", includeDraft);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken)) return null;

        return new CourseDetail(MapCourse(reader), reader.GetString("description"), reader.GetBoolean("is_published"));
    }

    public async Task<IReadOnlyList<CourseSummary>> GetEnrolledAsync(long userId, CancellationToken cancellationToken)
    {
        await using var connection = await connectionFactory.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = $"""
            {CourseProjection}
            INNER JOIN enrolments e ON e.course_id = c.id
            WHERE e.user_id = @userId AND c.is_published = TRUE
            ORDER BY c.title, c.id;
            """;
        command.Parameters.AddWithValue("@userId", userId);
        return await ReadCoursesAsync(command, cancellationToken);
    }

    public async Task<IReadOnlyList<DiscussionSummary>> GetRecentAsync(CancellationToken cancellationToken)
    {
        await using var connection = await connectionFactory.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT t.id, t.title, u.display_name,
                   (SELECT COUNT(*) FROM forum_replies r WHERE r.thread_id = t.id) AS reply_count,
                   t.last_activity_at_utc
            FROM forum_threads t
            INNER JOIN users u ON u.id = t.author_id
            LEFT JOIN courses c ON c.id = t.course_id
            WHERE t.course_id IS NULL OR c.is_published = TRUE
            ORDER BY t.is_pinned DESC, t.last_activity_at_utc DESC, t.id DESC
            LIMIT 50;
            """;
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var discussions = new List<DiscussionSummary>();
        while (await reader.ReadAsync(cancellationToken))
        {
            discussions.Add(new DiscussionSummary(reader.GetInt64("id"), reader.GetString("title"), reader.GetString("display_name"), reader.GetInt32("reply_count"), DateTime.SpecifyKind(reader.GetDateTime("last_activity_at_utc"), DateTimeKind.Utc)));
        }

        return discussions;
    }

    public async Task<DashboardSummary> GetSummaryAsync(CancellationToken cancellationToken)
    {
        await using var connection = await connectionFactory.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT (SELECT COUNT(*) FROM users u INNER JOIN roles r ON r.id = u.role_id WHERE r.name = 'Member') AS members,
                   (SELECT COUNT(*) FROM courses WHERE is_published = TRUE) AS published_courses,
                   (SELECT COUNT(*) FROM courses WHERE is_published = FALSE) AS draft_courses,
                   (SELECT COUNT(*) FROM lessons) AS lessons;
            """;
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        await reader.ReadAsync(cancellationToken);
        return new DashboardSummary(reader.GetInt32("members"), reader.GetInt32("published_courses"), reader.GetInt32("draft_courses"), reader.GetInt32("lessons"));
    }

    private static async Task<IReadOnlyList<CourseSummary>> ReadCoursesAsync(MySqlCommand command, CancellationToken cancellationToken)
    {
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var courses = new List<CourseSummary>();
        while (await reader.ReadAsync(cancellationToken)) courses.Add(MapCourse(reader));

        return courses;
    }

    private static CourseSummary MapCourse(MySqlDataReader reader) => new(
        reader.GetInt64("id"), reader.GetString("title"), reader.GetString("summary"),
        reader.GetString("category_name"), reader.GetString("level"), reader.GetInt32("lesson_count"),
        reader.GetInt32("duration_minutes"), reader.GetString("artwork_key"));
}
