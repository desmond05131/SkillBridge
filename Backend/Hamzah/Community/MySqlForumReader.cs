using SkillBridge.Web.Features.Community;
using SkillBridge.Web.Infrastructure.Data;

namespace SkillBridge.Web.Infrastructure.Community;

public sealed class MySqlForumReader(MySqlConnectionFactory connectionFactory) : IForumReader
{
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
}
