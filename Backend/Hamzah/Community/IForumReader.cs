namespace SkillBridge.Web.Features.Community;

public sealed record DiscussionSummary(long Id, string Title, string AuthorName, int ReplyCount, DateTime UpdatedAt);

public interface IForumReader
{
    Task<IReadOnlyList<DiscussionSummary>> GetRecentAsync(CancellationToken cancellationToken);
}
