using MySqlConnector;
using SkillBridge.Web.Features.Courses;
using SkillBridge.Web.Infrastructure.Data;

namespace SkillBridge.Web.Infrastructure.Courses;

public sealed class MySqlCourseReader(MySqlConnectionFactory connectionFactory) : ICourseReader
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
