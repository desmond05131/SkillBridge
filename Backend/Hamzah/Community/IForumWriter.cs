namespace SkillBridge.Web.Features.Community;

public interface IForumWriter
{
    Task<long> CreateThreadAsync(long authorId, string title, string body, CancellationToken cancellationToken);

    Task<bool> UpdateThreadAsync(long threadId, long requesterId, bool isAdmin, string title, string body, CancellationToken cancellationToken);

    Task<bool> DeleteThreadAsync(long threadId, long requesterId, bool isAdmin, CancellationToken cancellationToken);

    Task<bool> CreateReplyAsync(long threadId, long authorId, string body, CancellationToken cancellationToken);

    Task<bool> DeleteReplyAsync(long replyId, long requesterId, bool isAdmin, CancellationToken cancellationToken);

    Task SetModerationAsync(long threadId, bool isPinned, bool isLocked, CancellationToken cancellationToken);
    Task<bool> UpdateReplyAsync(long threadId, long replyId, long userId, bool isAdmin, string body, CancellationToken cancellationToken);
}
