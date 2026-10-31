using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SkillBridge.Web.Features.Courses;
using SkillBridge.Web.Infrastructure.Data;

namespace SkillBridge.Web.Pages.Courses;

public sealed class IndexModel(ICourseReader courses, MySqlConnectionFactory database) : PageModel
{
    [BindProperty(SupportsGet = true)]
    [StringLength(100)]
    public string? Search { get; set; }

    [BindProperty(SupportsGet = true), StringLength(80)]
    public string? Category { get; set; }

    [BindProperty(SupportsGet = true), RegularExpression("^(title|popular|newest)$")]
    public string? Sort { get; set; } = "popular";

    public List<string> Categories { get; } = [];

    public IReadOnlyList<CourseSummary> Courses { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        await using var connection = await database.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT name FROM categories ORDER BY name";
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
            Categories.Add(reader.GetString(0));

        Sort ??= "popular";
        if (!ModelState.IsValid) return Page();

        Courses = await courses.GetPublishedAsync(Search, cancellationToken, Category, Sort);
        return Page();
    }
}
