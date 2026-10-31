using MySqlConnector;
using SkillBridge.Web.Features.Community;
using SkillBridge.Web.Infrastructure.Data;

namespace SkillBridge.Web.Infrastructure.Community;

public sealed class MySqlForumWriter(MySqlConnectionFactory connectionFactory) : IForumWriter
{
    public async Task<long> CreateThreadAsync(long authorId, string title, string body, CancellationToken cancellationToken)
    {
        await using var connection = await connectionFactory.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO forum_threads (author_id, title, body)
            VALUES (@authorId, @title, @body);
            SELECT LAST_INSERT_ID();
            """;
        command.Parameters.AddWithValue("@authorId", authorId);
        command.Parameters.AddWithValue("@title", title);
        command.Parameters.AddWithValue("@body", body);
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt64(result!);
    }

    public async Task<bool> UpdateThreadAsync(long threadId, long requesterId, bool isAdmin, string title, string body, CancellationToken cancellationToken)
    {
        await using var connection = await connectionFactory.OpenAsync(cancellationToken);
        if (!await IsAuthorOrAdminAsync(connection, "SELECT author_id FROM forum_threads WHERE id = @entityId;", threadId, requesterId, isAdmin, cancellationToken))
            return false;

        await using var command = connection.CreateCommand();
        command.CommandText = "UPDATE forum_threads SET title = @title, body = @body WHERE id = @threadId;";
        command.Parameters.AddWithValue("@threadId", threadId);
        command.Parameters.AddWithValue("@title", title);
        command.Parameters.AddWithValue("@body", body);
        return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
    }

    public async Task<bool> DeleteThreadAsync(long threadId, long requesterId, bool isAdmin, CancellationToken cancellationToken)
    {
        await using var connection = await connectionFactory.OpenAsync(cancellationToken);
        if (!await IsAuthorOrAdminAsync(connection, "SELECT author_id FROM forum_threads WHERE id = @entityId;", threadId, requesterId, isAdmin, cancellationToken))
            return false;

        // Foreign keys use ON DELETE RESTRICT, so replies must be removed before the thread.
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        await using (var command = connection.CreateCommand())
        {
            command.Transaction = transaction;
            command.CommandText = "DELETE FROM forum_replies WHERE thread_id = @threadId;";
            command.Parameters.AddWithValue("@threadId", threadId);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        long deleted;
        await using (var command = connection.CreateCommand())
        {
            command.Transaction = transaction;
            command.CommandText = "DELETE FROM forum_threads WHERE id = @threadId;";
            command.Parameters.AddWithValue("@threadId", threadId);
            deleted = await command.ExecuteNonQueryAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
        return deleted > 0;
    }

    public async Task<bool> CreateReplyAsync(long threadId, long authorId, string body, CancellationToken cancellationToken)
    {
        await using var connection = await connectionFactory.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        long inserted;
        await using (var command = connection.CreateCommand())
        {
            command.Transaction = transaction;
            command.CommandText = """
                INSERT INTO forum_replies (thread_id, author_id, body)
                SELECT @threadId, @authorId, @body
                WHERE EXISTS (
                    SELECT 1 FROM forum_threads
                    WHERE id = @threadId AND is_locked = FALSE
                );
                """;
            command.Parameters.AddWithValue("@threadId", threadId);
            command.Parameters.AddWithValue("@authorId", authorId);
            command.Parameters.AddWithValue("@body", body);
            inserted = await command.ExecuteNonQueryAsync(cancellationToken);
        }

        if (inserted > 0)
        {
            await using var touch = connection.CreateCommand();
            touch.Transaction = transaction;
            touch.CommandText = "UPDATE forum_threads SET last_activity_at_utc = UTC_TIMESTAMP() WHERE id = @threadId;";
            touch.Parameters.AddWithValue("@threadId", threadId);
            await touch.ExecuteNonQueryAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
        return inserted > 0;
    }

    public async Task<bool> DeleteReplyAsync(long replyId, long requesterId, bool isAdmin, CancellationToken cancellationToken)
    {
        await using var connection = await connectionFactory.OpenAsync(cancellationToken);

        long threadId;
        long authorId;
        await using (var lookup = connection.CreateCommand())
        {
            lookup.CommandText = "SELECT thread_id, author_id FROM forum_replies WHERE id = @replyId;";
            lookup.Parameters.AddWithValue("@replyId", replyId);
            await using var reader = await lookup.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken)) return false;
            threadId = reader.GetInt64("thread_id");
            authorId = reader.GetInt64("author_id");
        }

        if (!isAdmin && authorId != requesterId) return false;

        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        await using (var command = connection.CreateCommand())
        {
            command.Transaction = transaction;
            command.CommandText = "DELETE FROM forum_replies WHERE id = @replyId;";
            command.Parameters.AddWithValue("@replyId", replyId);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        await using (var refresh = connection.CreateCommand())
        {
            refresh.Transaction = transaction;
            refresh.CommandText = """
                UPDATE forum_threads
                SET last_activity_at_utc = GREATEST(
                        created_at_utc,
                        COALESCE((SELECT MAX(r.created_at_utc) FROM forum_replies r WHERE r.thread_id = @threadId), created_at_utc))
                WHERE id = @threadId;
                """;
            refresh.Parameters.AddWithValue("@threadId", threadId);
            await refresh.ExecuteNonQueryAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    public async Task SetModerationAsync(long threadId, bool isPinned, bool isLocked, CancellationToken cancellationToken)
    {
        await using var connection = await connectionFactory.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "UPDATE forum_threads SET is_pinned = @isPinned, is_locked = @isLocked WHERE id = @threadId;";
        command.Parameters.AddWithValue("@threadId", threadId);
        command.Parameters.AddWithValue("@isPinned", isPinned);
        command.Parameters.AddWithValue("@isLocked", isLocked);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task<bool> IsAuthorOrAdminAsync(MySqlConnection connection, string lookupSql, long entityId, long requesterId, bool isAdmin, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = lookupSql;
        command.Parameters.AddWithValue("@entityId", entityId);
        var authorId = await command.ExecuteScalarAsync(cancellationToken);
        return authorId is not null && (isAdmin || Convert.ToInt64(authorId) == requesterId);
    }
    public async Task<bool> UpdateReplyAsync(long threadId, long replyId, long userId, bool isAdmin, string body, CancellationToken cancellationToken)
    {
        await using var connection = await connectionFactory.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            UPDATE forum_replies r JOIN forum_threads t ON t.id=r.thread_id SET r.body=@body
            WHERE r.id=@reply AND r.thread_id=@thread AND (r.author_id=@user OR @admin=TRUE)
              AND (t.is_locked=FALSE OR @admin=TRUE);
            """;
        command.Parameters.AddWithValue("@reply", replyId);
        command.Parameters.AddWithValue("@thread", threadId);
        command.Parameters.AddWithValue("@user", userId);
        command.Parameters.AddWithValue("@admin", isAdmin);
        command.Parameters.AddWithValue("@body", body);
        return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
    }
}
