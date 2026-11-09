namespace SkillBridge.Web.Features.Community;

public sealed record DiscussionSummary(long Id, string Title, string AuthorName, int ReplyCount, DateTime UpdatedAt, bool IsPinned, bool IsLocked);

public sealed record ForumThreadDetail(long Id, string Title, string Body, long AuthorId, string AuthorName, long? CourseId, bool IsPinned, bool IsLocked, DateTime CreatedAtUtc, DateTime LastActivityAtUtc);

public sealed record ForumReply(long Id, string Body, long AuthorId, string AuthorName, DateTime CreatedAtUtc, DateTime UpdatedAtUtc);

public interface IForumReader
{
    Task<IReadOnlyList<DiscussionSummary>> GetRecentAsync(CancellationToken cancellationToken);

    Task<ForumThreadDetail?> GetThreadAsync(long threadId, CancellationToken cancellationToken);

    Task<IReadOnlyList<ForumReply>> GetRepliesAsync(long threadId, CancellationToken cancellationToken);

    Task<IReadOnlyList<DiscussionSummary>> GetAllAsync(CancellationToken cancellationToken, string? search = null, string? status = null);
}
