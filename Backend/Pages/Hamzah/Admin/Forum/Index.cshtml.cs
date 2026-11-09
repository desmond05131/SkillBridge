using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SkillBridge.Web.Features.Community;

namespace SkillBridge.Web.Pages.Hamzah.Admin;

// This whole folder is already restricted to the "Admin" policy by Program.cs:
// options.Conventions.AuthorizeFolder("/Hamzah/Admin", "Admin")
public sealed class IndexModel(IForumReader forum, IForumWriter forumWriter) : PageModel
{
    public IReadOnlyList<DiscussionSummary> Threads { get; private set; } = [];
    [BindProperty(SupportsGet = true), System.ComponentModel.DataAnnotations.StringLength(120)]
    public string? Search { get; set; }
    [BindProperty(SupportsGet = true), System.ComponentModel.DataAnnotations.RegularExpression("^(Open|Pinned|Locked)$", ErrorMessage = "Choose a valid discussion status.")]
    public string? Status { get; set; }

    [BindProperty]
    public long ModerateThreadId { get; set; }

    [BindProperty]
    public bool SetPinned { get; set; }

    [BindProperty]
    public bool SetLocked { get; set; }

    [BindProperty]
    public long DeleteThreadId { get; set; }

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return Page();
        Threads = await forum.GetAllAsync(cancellationToken, Search, Status);
        return Page();
    }

    public async Task<IActionResult> OnPostModerateAsync(CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return BadRequest();
        if (ModerateThreadId > 0)
            await forumWriter.SetModerationAsync(ModerateThreadId, SetPinned, SetLocked, cancellationToken);

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteThreadAsync(CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return BadRequest();
        if (DeleteThreadId > 0 && long.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            await forumWriter.DeleteThreadAsync(DeleteThreadId, userId, isAdmin: true, cancellationToken);

        return RedirectToPage();
    }
}
