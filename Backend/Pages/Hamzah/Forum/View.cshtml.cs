using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SkillBridge.Web.Features.Community;

namespace SkillBridge.Web.Pages.Hamzah.Forum;

public sealed class ViewModel(IForumReader forum, IForumWriter forumWriter) : PageModel
{
    public ForumThreadDetail Thread { get; private set; } = null!;

    public IReadOnlyList<ForumReply> Replies { get; private set; } = [];

    [BindProperty]
    [Required(ErrorMessage = "Please write a reply before posting.")]
    [StringLength(3000, MinimumLength = 2, ErrorMessage = "Replies must be between 2 and 3,000 characters.")]
    public string? ReplyBody { get; set; }

    [BindProperty]
    public long ReplyId { get; set; }

    public bool IsModerator => User.IsInRole("Admin");

    public bool IsThreadAuthor => TryUserId(out var userId) && Thread.AuthorId == userId;

    private bool TryUserId(out long userId) =>
        long.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out userId) && userId > 0;

    public async Task<IActionResult> OnGetAsync(long id, CancellationToken cancellationToken)
    {
        if (id <= 0) return NotFound();

        var thread = await forum.GetThreadAsync(id, cancellationToken);
        if (thread is null) return NotFound();

        Thread = thread;
        Replies = await forum.GetRepliesAsync(id, cancellationToken);
        return Page();
    }


    public async Task<IActionResult> OnPostReplyAsync(long id, CancellationToken cancellationToken)
    {
        var thread = await forum.GetThreadAsync(id, cancellationToken);
        if (thread is null) return NotFound();
        Thread = thread;

        ReplyBody = ReplyBody?.Trim();
        if (ReplyBody?.Length < 2) ModelState.AddModelError(nameof(ReplyBody), "Use at least 2 characters after trimming spaces.");
        if (thread.IsLocked)
            ModelState.AddModelError(string.Empty, "This discussion is locked, so no new replies can be posted.");

        if (!ModelState.IsValid)
        {
            Replies = await forum.GetRepliesAsync(id, cancellationToken);
            return Page();
        }

        if (!TryUserId(out var userId)) return Challenge();

        var posted = await forumWriter.CreateReplyAsync(id, userId, ReplyBody!.Trim(), cancellationToken);
        if (!posted)
        {
            ModelState.AddModelError(string.Empty, "This discussion is locked, so no new replies can be posted.");
            Replies = await forum.GetRepliesAsync(id, cancellationToken);
            return Page();
        }

        return RedirectToPage(new { id });
    }


    public async Task<IActionResult> OnPostDeleteThreadAsync(long id, CancellationToken cancellationToken)
    {
        if (!TryUserId(out var userId)) return Challenge();

        var deleted = await forumWriter.DeleteThreadAsync(id, userId, IsModerator, cancellationToken);
        if (!deleted) return Forbid();

        return RedirectToPage("/Hamzah/Forum/Index");
    }


    public async Task<IActionResult> OnPostDeleteReplyAsync(long id, CancellationToken cancellationToken)
    {
        if (!TryUserId(out var userId)) return Challenge();

        var deleted = await forumWriter.DeleteReplyAsync(ReplyId, userId, IsModerator, cancellationToken);
        if (!deleted) return Forbid();

        return RedirectToPage(new { id });
    }
    [BindProperty] public string? EditedReplyBody { get; set; }
    [TempData] public string? StatusMessage { get; set; }
    public async Task<IActionResult> OnPostEditReplyAsync(long id, CancellationToken cancellationToken)
    {
        if (!TryUserId(out var userId)) return Challenge();
        var body = EditedReplyBody?.Trim();
        if (body is null || body.Length < 2 || body.Length > 3000)
        {
            StatusMessage = "Replies must contain 2 to 3,000 characters.";
            return RedirectToPage(new { id });
        }
        if (!await forumWriter.UpdateReplyAsync(id, ReplyId, userId, IsModerator, body, cancellationToken)) return Forbid();
        StatusMessage = "Reply updated.";
        return RedirectToPage(new { id });
    }
}
