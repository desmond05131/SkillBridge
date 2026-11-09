using SkillBridge.Web.Features.Community;
using SkillBridge.Web.Infrastructure.Data;
using MySqlConnector;

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
                   t.last_activity_at_utc, t.is_pinned, t.is_locked
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
            discussions.Add(ReadSummary(reader));
        }

        return discussions;
    }

    public async Task<IReadOnlyList<DiscussionSummary>> GetAllAsync(CancellationToken cancellationToken, string? search = null, string? status = null)
    {
        await using var connection = await connectionFactory.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT t.id, t.title, u.display_name,
                   (SELECT COUNT(*) FROM forum_replies r WHERE r.thread_id = t.id) AS reply_count,
                   t.last_activity_at_utc, t.is_pinned, t.is_locked
            FROM forum_threads t
            INNER JOIN users u ON u.id = t.author_id
            WHERE (@search = '' OR LOCATE(@search,t.title)>0 OR LOCATE(@search,u.display_name)>0)
              AND (@status = '' OR (@status='Pinned' AND t.is_pinned=TRUE)
                  OR (@status='Locked' AND t.is_locked=TRUE) OR (@status='Open' AND t.is_locked=FALSE))
            ORDER BY t.last_activity_at_utc DESC, t.id DESC
            LIMIT 500;
            """;
        command.Parameters.AddWithValue("@search", search?.Trim() ?? "");
        command.Parameters.AddWithValue("@status", status ?? "");
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var discussions = new List<DiscussionSummary>();
        while (await reader.ReadAsync(cancellationToken))
        {
            discussions.Add(ReadSummary(reader));
        }

        return discussions;
    }

    public async Task<ForumThreadDetail?> GetThreadAsync(long threadId, CancellationToken cancellationToken)
    {
        await using var connection = await connectionFactory.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT t.id, t.title, t.body, t.author_id, u.display_name, t.course_id,
                   t.is_pinned, t.is_locked, t.created_at_utc, t.last_activity_at_utc
            FROM forum_threads t
            INNER JOIN users u ON u.id = t.author_id
            LEFT JOIN courses c ON c.id=t.course_id
            WHERE t.id = @threadId AND (t.course_id IS NULL OR c.is_published=TRUE);
            """;
        command.Parameters.AddWithValue("@threadId", threadId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken)) return null;

        return new ForumThreadDetail(
            reader.GetInt64("id"), reader.GetString("title"), reader.GetString("body"),
            reader.GetInt64("author_id"), reader.GetString("display_name"),
            reader.IsDBNull(reader.GetOrdinal("course_id")) ? null : reader.GetInt64("course_id"),
            reader.GetBoolean("is_pinned"), reader.GetBoolean("is_locked"),
            DateTime.SpecifyKind(reader.GetDateTime("created_at_utc"), DateTimeKind.Utc),
            DateTime.SpecifyKind(reader.GetDateTime("last_activity_at_utc"), DateTimeKind.Utc));
    }

    public async Task<IReadOnlyList<ForumReply>> GetRepliesAsync(long threadId, CancellationToken cancellationToken)
    {
        await using var connection = await connectionFactory.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT r.id, r.body, r.author_id, u.display_name, r.created_at_utc, r.updated_at_utc
            FROM forum_replies r
            INNER JOIN users u ON u.id = r.author_id
            WHERE r.thread_id = @threadId
            ORDER BY r.created_at_utc ASC, r.id ASC;
            """;
        command.Parameters.AddWithValue("@threadId", threadId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var replies = new List<ForumReply>();
        while (await reader.ReadAsync(cancellationToken))
        {
            replies.Add(new ForumReply(
                reader.GetInt64("id"), reader.GetString("body"),
                reader.GetInt64("author_id"), reader.GetString("display_name"),
                DateTime.SpecifyKind(reader.GetDateTime("created_at_utc"), DateTimeKind.Utc),
                DateTime.SpecifyKind(reader.GetDateTime("updated_at_utc"), DateTimeKind.Utc)));
        }

        return replies;
    }

    private static DiscussionSummary ReadSummary(MySqlDataReader reader) =>
        new(reader.GetInt64("id"), reader.GetString("title"), reader.GetString("display_name"),
            reader.GetInt32("reply_count"),
            DateTime.SpecifyKind(reader.GetDateTime("last_activity_at_utc"), DateTimeKind.Utc),
            reader.GetBoolean("is_pinned"), reader.GetBoolean("is_locked"));
}
