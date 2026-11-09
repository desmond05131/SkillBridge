using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MySqlConnector;
using SkillBridge.Web.Infrastructure.Data;

namespace SkillBridge.Web.Pages.ChangZhe.Courses.Admin;

[Authorize(Roles = "Admin")]
public class CreateModel(MySqlConnectionFactory database, IWebHostEnvironment environment) : PageModel
{
    private const long MaxThumbnailBytes = 2 * 1024 * 1024;
    private const string UploadSubfolder = "uploads/courses";

    [BindProperty]
    public long? CourseId { get; set; }
    public bool IsEdit => CourseId.HasValue;
    public string? Message { get; set; }
    public string? ExistingThumbnailUrl { get; private set; }
    public List<string> Categories { get; private set; } = new();

    [BindProperty]
    public CourseInput Input { get; set; } = new();

    [BindProperty]
    public IFormFile? Thumbnail { get; set; }

    public class CourseInput
    {
        [Required, StringLength(100, MinimumLength = 3)]
        public string Title { get; set; } = "";

        [Required]
        public string Category { get; set; } = "";

        [Required, StringLength(180, MinimumLength = 30)]
        public string Summary { get; set; } = "";

        [Required, StringLength(2000, MinimumLength = 80)]
        public string Description { get; set; } = "";

        [Required]
        public string Level { get; set; } = "Beginner";

        [Range(5, 600)]
        public int DurationMinutes { get; set; } = 30;

        public bool Published { get; set; }
    }

    private async Task LoadCategoriesAsync(CancellationToken ct)
    {
        Categories.Clear();
        await using var connection = await database.OpenAsync(ct);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT name FROM categories ORDER BY name";
        await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
            Categories.Add(reader.GetString(0));
    }

    public async Task<IActionResult> OnGetAsync(long? id, CancellationToken ct)
    {
        await LoadCategoriesAsync(ct);
        CourseId = id;
        if (id is null) return Page();
        if (id.Value <= 0) return NotFound();

        await using var connection = await database.OpenAsync(ct);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT c.title, cat.name, c.summary, c.description, c.level,
                   c.duration_minutes, c.is_published, c.thumbnail_url
            FROM courses c
            INNER JOIN categories cat ON cat.id = c.category_id
            WHERE c.id = @id
            """;
        command.Parameters.AddWithValue("@id", id.Value);
        await using var reader = await command.ExecuteReaderAsync(ct);
        if (!await reader.ReadAsync(ct)) return NotFound();

        Input = new CourseInput
        {
            Title = reader.GetString(0),
            Category = reader.GetString(1),
            Summary = reader.GetString(2),
            Description = reader.GetString(3),
            Level = reader.GetString(4),
            DurationMinutes = reader.GetInt32(5),
            Published = reader.GetBoolean(6)
        };
        ExistingThumbnailUrl = reader.IsDBNull(7) ? null : reader.GetString(7);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        await LoadCategoriesAsync(ct);

        Input.Title = Input.Title?.Trim() ?? "";
        Input.Category = Input.Category?.Trim() ?? "";
        Input.Summary = Input.Summary?.Trim() ?? "";
        Input.Description = Input.Description?.Trim() ?? "";

        // Preserve binding failures (for example an invalid ID or duration) before revalidating trimmed text.
        foreach (var field in new[] { "Title", "Category", "Summary", "Description" })
            ModelState.Remove($"Input.{field}");
        TryValidateModel(Input, nameof(Input));

        if (!Input.Title.Any(char.IsLetter))
            ModelState.AddModelError("Input.Title", "Include words in the course title.");

        if (!Categories.Contains(Input.Category))
            ModelState.AddModelError("Input.Category", "Please choose a valid category.");

        if (Input.Level is not ("Beginner" or "Intermediate" or "Advanced"))
            ModelState.AddModelError("Input.Level", "Please choose a valid course level.");

        if (CourseId.HasValue && CourseId.Value <= 0)
            ModelState.AddModelError("", "Invalid course ID.");

        string? extension = null;
        if (Thumbnail is { Length: > 0 })
        {
            if (Thumbnail.Length > MaxThumbnailBytes)
                ModelState.AddModelError(nameof(Thumbnail), "Thumbnail must be 2 MB or smaller.");
            else
            {
                extension = await DetectImageExtensionAsync(Thumbnail, ct);
                if (extension is null)
                    ModelState.AddModelError(nameof(Thumbnail), "Upload a valid JPG, PNG or WebP image.");
            }
        }
        else if (Thumbnail is not null)
        {
            ModelState.AddModelError(nameof(Thumbnail), "The selected image is empty.");
        }

        await using var connection = await database.OpenAsync(ct);
        if (CourseId.HasValue)
        {
            await using var existingCommand = connection.CreateCommand();
            existingCommand.CommandText = "SELECT thumbnail_url FROM courses WHERE id = @id";
            existingCommand.Parameters.AddWithValue("@id", CourseId.Value);
            var existing = await existingCommand.ExecuteScalarAsync(ct);
            if (existing is null) return NotFound();
            ExistingThumbnailUrl = existing == DBNull.Value ? null : Convert.ToString(existing);
        }

        if (!ModelState.IsValid) return Page();

        string? newThumbnailUrl = null;
        if (Thumbnail is { Length: > 0 } && extension is not null)
        {
            var root = environment.WebRootPath ?? Path.Combine(environment.ContentRootPath, "Frontend", "wwwroot");
            var directory = Path.Combine(root, "uploads", "courses");
            Directory.CreateDirectory(directory);
            var filename = $"{Guid.NewGuid():N}{extension}";
            var filePath = Path.Combine(directory, filename);
            try
            {
                await using var stream = new FileStream(filePath, FileMode.CreateNew);
                await Thumbnail.CopyToAsync(stream, ct);
                newThumbnailUrl = $"/{UploadSubfolder}/{filename}";
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                if (System.IO.File.Exists(filePath)) System.IO.File.Delete(filePath);
                ModelState.AddModelError(nameof(Thumbnail), "Unable to save the thumbnail. Please try again.");
                return Page();
            }
        }

        try
        {
            await using var command = connection.CreateCommand();
            if (CourseId is null)
            {
                command.CommandText = """
                    INSERT INTO courses
                    (category_id, title, normalized_title, summary, description,
                     level, is_published, duration_minutes, thumbnail_url)
                    SELECT cat.id, @title, @normalized, @summary, @description,
                           @level, @published, @duration, @thumbnail
                    FROM categories cat WHERE cat.name = @category
                    """;
            }
            else
            {
                command.CommandText = """
                    UPDATE courses c
                    INNER JOIN categories cat ON cat.name = @category
                    SET c.category_id = cat.id,
                        c.title = @title,
                        c.normalized_title = @normalized,
                        c.summary = @summary,
                        c.description = @description,
                        c.level = @level,
                        c.is_published = @published,
                        c.duration_minutes = @duration,
                        c.thumbnail_url = COALESCE(@thumbnail, c.thumbnail_url)
                    WHERE c.id = @id
                    """;
                command.Parameters.AddWithValue("@id", CourseId.Value);
            }

            command.Parameters.AddWithValue("@title", Input.Title);
            command.Parameters.AddWithValue("@normalized", Input.Title.ToUpperInvariant());
            command.Parameters.AddWithValue("@summary", Input.Summary);
            command.Parameters.AddWithValue("@description", Input.Description);
            command.Parameters.AddWithValue("@level", Input.Level);
            command.Parameters.AddWithValue("@published", Input.Published);
            command.Parameters.AddWithValue("@duration", Input.DurationMinutes);
            command.Parameters.AddWithValue("@category", Input.Category);
            command.Parameters.AddWithValue("@thumbnail", (object?)newThumbnailUrl ?? DBNull.Value);

            int affected = await command.ExecuteNonQueryAsync(ct);
            if (affected == 0)
            {
                if (CourseId.HasValue)
                {
                    await using var check = connection.CreateCommand();
                    check.CommandText = "SELECT COUNT(*) FROM courses WHERE id = @id";
                    check.Parameters.AddWithValue("@id", CourseId.Value);
                    if (Convert.ToInt64(await check.ExecuteScalarAsync(ct)) == 0)
                    {
                        DeleteUploadedFile(newThumbnailUrl);
                        return NotFound();
                    }
                }
                else
                {
                    DeleteUploadedFile(newThumbnailUrl);
                    ModelState.AddModelError("Input.Category", "The selected category was not found.");
                    return Page();
                }
            }
        }
        catch (MySqlException ex) when (ex.Number == 1062)
        {
            DeleteUploadedFile(newThumbnailUrl);
            ModelState.AddModelError("Input.Title", "A course with this title already exists.");
            return Page();
        }
        catch
        {
            DeleteUploadedFile(newThumbnailUrl);
            throw;
        }

        // Only delete a previous image after the database update succeeds.
        if (CourseId.HasValue && newThumbnailUrl is not null && ExistingThumbnailUrl != newThumbnailUrl)
        {
            try { DeleteUploadedFile(ExistingThumbnailUrl); }
            catch (IOException) { /* Keep successful course update even if cleanup fails. */ }
            catch (UnauthorizedAccessException) { /* Old file can be cleaned up later. */ }
        }

        TempData["CourseMessage"] = IsEdit ? "Course updated successfully." : "Course created successfully.";
        return RedirectToPage("/ChangZhe/Courses/Admin/Index");
    }

    private void DeleteUploadedFile(string? url)
    {
        if (string.IsNullOrEmpty(url) ||
            !url.StartsWith($"/{UploadSubfolder}/", StringComparison.Ordinal)) return;
        var filename = Path.GetFileName(url);
        if (string.IsNullOrWhiteSpace(filename)) return;
        var root = environment.WebRootPath ?? Path.Combine(environment.ContentRootPath, "Frontend", "wwwroot");
        var path = Path.Combine(root, "uploads", "courses", filename);
        if (System.IO.File.Exists(path)) System.IO.File.Delete(path);
    }

    private static async Task<string?> DetectImageExtensionAsync(IFormFile file, CancellationToken ct)
    {
        byte[] header = new byte[12];
        await using var stream = file.OpenReadStream();
        int count = 0;
        while (count < header.Length)
        {
            int read = await stream.ReadAsync(header.AsMemory(count, header.Length - count), ct);
            if (read == 0) break;
            count += read;
        }
        if (count >= 3 && header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF)
            return ".jpg";
        if (count >= 8 && header.AsSpan(0, 8).SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }))
            return ".png";
        if (count >= 12 && header.AsSpan(0, 4).SequenceEqual("RIFF"u8) &&
            header.AsSpan(8, 4).SequenceEqual("WEBP"u8))
            return ".webp";
        return null;
    }
}
