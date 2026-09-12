using Microsoft.AspNetCore.Mvc.RazorPages;
using SkillBridge.Web.Features.Community;

namespace SkillBridge.Web.Pages.Forum;

public sealed class IndexModel(IForumReader forum) : PageModel
{
    public IReadOnlyList<DiscussionSummary> Discussions { get; private set; } = [];

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Discussions = await forum.GetRecentAsync(cancellationToken);
    }
}
