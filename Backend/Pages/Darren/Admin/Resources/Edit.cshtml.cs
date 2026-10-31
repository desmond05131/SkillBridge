using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MySqlConnector;
using SkillBridge.Web.Features.Lessons;
using LessonManager = SkillBridge.Backend.Darren.Lessons.ILessonReader;
namespace SkillBridge.Backend.Pages.Darren.Admin.Resources;

[RequestSizeLimit(55 * 1024 * 1024)]
[RequestFormLimits(MultipartBodyLengthLimit = 55 * 1024 * 1024)]
public class EditModel(ResourceStore resources, LessonManager lessons) : PageModel
{
    [BindProperty] public ResourceInput Resource { get; set; } = new();
    [BindProperty] public IFormFile? UploadedFile { get; set; }
    [BindProperty] public IFormFile? CaptionsFile { get; set; }
    public List<SkillBridge.Backend.Darren.Lessons.LessonOption> Lessons { get; private set; } = [];
    public bool IsEditMode => Resource.Id > 0;
    public async Task<IActionResult> OnGetAsync(long? id, CancellationToken cancellationToken)
    {
        Lessons = await lessons.GetLessonOptionsAsync();
        if (id is null) return Page();
        var file = (await resources.ListAsync(id, cancellationToken)).SingleOrDefault();
        if (file is null) return NotFound();
        Resource = new() { Id = file.Id, LessonId = file.LessonId, Title = file.Title, Description = file.Description, Type = file.Type, Transcript = file.Transcript };
        return Page();
    }
    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        Lessons = await lessons.GetLessonOptionsAsync();
        if (!Lessons.Any(lesson => lesson.Id == Resource.LessonId)) ModelState.AddModelError("Resource.LessonId", "Choose an existing lesson.");
        if (string.IsNullOrWhiteSpace(Resource.Title) || Resource.Title.Trim().Length < 3) ModelState.AddModelError("Resource.Title", "Enter at least 3 characters.");
        if (string.IsNullOrWhiteSpace(Resource.Transcript)) ModelState.AddModelError("Resource.Transcript", "Provide an accessible text equivalent.");
        if (!ModelState.IsValid) return Page();
        try { await resources.SaveAsync(Resource, UploadedFile, CaptionsFile, cancellationToken); }
        catch (ValidationException exception) { ModelState.AddModelError(string.Empty, exception.Message); return Page(); }
        catch (MySqlException exception) when (exception.Number is 1452 or 3819 or 4025)
        { ModelState.AddModelError(string.Empty, "Check the lesson and field limits, then try again."); return Page(); }
        TempData["StatusMessage"] = "Resource saved.";
        return RedirectToPage("Index");
    }
}
