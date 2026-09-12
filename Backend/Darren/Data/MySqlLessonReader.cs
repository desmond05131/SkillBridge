using MySqlConnector;
using SkillBridge.Web.Features.Lessons;
using SkillBridge.Web.Infrastructure.Data;

namespace SkillBridge.Web.Infrastructure.Content;

public sealed class MySqlLessonReader(MySqlConnectionFactory connectionFactory) : ILessonReader
{
    public async Task<LessonDetail?> GetByIdAsync(long id, long userId, bool isAdmin, CancellationToken cancellationToken)
    {
        await using var connection = await connectionFactory.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT l.id, l.course_id, c.title AS course_title, l.title, l.sequence_number,
                   l.duration_minutes, c.is_published, l.summary, l.body
            FROM lessons l
            INNER JOIN courses c ON c.id = l.course_id
            WHERE l.id = @id
              AND (@isAdmin = TRUE OR (c.is_published = TRUE AND EXISTS (
                  SELECT 1 FROM enrolments e WHERE e.course_id = c.id AND e.user_id = @userId
              )));
            """;
        command.Parameters.AddWithValue("@id", id);
        command.Parameters.AddWithValue("@userId", userId);
        command.Parameters.AddWithValue("@isAdmin", isAdmin);

        LessonDetail lesson;
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            if (!await reader.ReadAsync(cancellationToken)) return null;

            lesson = new LessonDetail(MapLesson(reader), reader.GetString("summary"), reader.GetString("body"), []);
        }

        var resources = await ReadResourcesAsync(connection, id, userId, isAdmin, cancellationToken);
        return lesson with { Resources = resources };
    }

    public async Task<IReadOnlyList<LessonSummary>> GetForAdministrationAsync(CancellationToken cancellationToken)
    {
        await using var connection = await connectionFactory.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT l.id, l.course_id, c.title AS course_title, l.title,
                   l.sequence_number, l.duration_minutes, c.is_published
            FROM lessons l
            INNER JOIN courses c ON c.id = l.course_id
            ORDER BY c.title, c.id, l.sequence_number, l.id;
            """;
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var lessons = new List<LessonSummary>();
        while (await reader.ReadAsync(cancellationToken)) lessons.Add(MapLesson(reader));

        return lessons;
    }

    private static async Task<IReadOnlyList<LessonResource>> ReadResourcesAsync(MySqlConnection connection, long lessonId, long userId, bool isAdmin, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT r.title, r.description, r.resource_type, r.size_bytes
            FROM resources r
            INNER JOIN lessons l ON l.id = r.lesson_id
            INNER JOIN courses c ON c.id = l.course_id
            WHERE l.id = @lessonId
              AND (@isAdmin = TRUE OR (c.is_published = TRUE AND EXISTS (
                  SELECT 1 FROM enrolments e WHERE e.course_id = c.id AND e.user_id = @userId
              )))
            ORDER BY r.title, r.id;
            """;
        command.Parameters.AddWithValue("@lessonId", lessonId);
        command.Parameters.AddWithValue("@userId", userId);
        command.Parameters.AddWithValue("@isAdmin", isAdmin);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var resources = new List<LessonResource>();
        while (await reader.ReadAsync(cancellationToken))
            resources.Add(new LessonResource(reader.GetString("title"), reader.GetString("description"), reader.GetString("resource_type"), reader.GetInt64("size_bytes")));

        return resources;
    }

    private static LessonSummary MapLesson(MySqlDataReader reader) => new(
        reader.GetInt64("id"), reader.GetInt64("course_id"), reader.GetString("course_title"),
        reader.GetString("title"), reader.GetInt32("sequence_number"), reader.GetInt32("duration_minutes"),
        reader.GetBoolean("is_published"));
}
