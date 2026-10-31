using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SkillBridge.Web.Features.Lessons;
namespace SkillBridge.Web.Pages.Resources;

public sealed class FileModel(ResourceStore resources, ILessonReader lessons) : PageModel
{
    public async Task<IActionResult> OnGetAsync(long id, bool download, bool captions, CancellationToken cancellationToken)
    {
        if (!long.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)) return Challenge();
        var resource = (await resources.ListAsync(id, cancellationToken)).SingleOrDefault();
        if (resource is null) return NotFound();
        if (await lessons.GetByIdAsync(resource.LessonId, userId, User.IsInRole("Admin"), cancellationToken) is null) return NotFound();
        var key = captions ? resource.CaptionsKey : resource.StorageKey;
        if (key is null) return NotFound();
        var path = resources.FilePath(key);
        if (path is null) return NotFound();
        Response.Headers.CacheControl = "private, no-store";
        return new PhysicalFileResult(path, captions ? "text/vtt; charset=utf-8" : resource.ContentType)
        {
            EnableRangeProcessing = !captions,
            FileDownloadName = download ? resource.Filename : null
        };
    }
}
