using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.RegularExpressions;
using MySqlConnector;
using SkillBridge.Web.Infrastructure.Data;

namespace SkillBridge.Web.Features.Lessons;

public sealed class ResourceInput
{
    public long Id { get; set; }
    [Range(1, long.MaxValue)] public long LessonId { get; set; }
    [Required, StringLength(120, MinimumLength = 3)] public string Title { get; set; } = "";
    [Required, RegularExpression("^(Pdf|Video|Audio)$")] public string Type { get; set; } = "Pdf";
    [StringLength(500)] public string Description { get; set; } = "";
    [Required, StringLength(20000)] public string Transcript { get; set; } = "";
}

public sealed record ResourceFile(long Id, long LessonId, string LessonTitle, string Title, string Description,
    string Type, string StorageKey, string Filename, string ContentType, long Size, string Transcript, string? CaptionsKey);

public sealed class ResourceStore(MySqlConnectionFactory connections, IWebHostEnvironment environment, IConfiguration configuration)
{
    private string UploadDirectory => configuration["Storage:ResourceDirectory"] ?? Path.Combine(environment.ContentRootPath, "App_Data", "resources");

    public string? FilePath(string key)
    {
        if (!Regex.IsMatch(key, @"\A[a-zA-Z0-9_-]+\.[a-z0-9]+\z")) return null;
        var uploaded = Path.Combine(UploadDirectory, key);
        if (File.Exists(uploaded)) return uploaded;
        var bundled = Path.Combine(environment.ContentRootPath, "database", "media", key);
        return File.Exists(bundled) ? bundled : null;
    }

    public async Task<List<ResourceFile>> ListAsync(long? id, CancellationToken cancellationToken)
    {
        await using var connection = await connections.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT r.*, l.title AS lesson_title FROM resources r JOIN lessons l ON l.id=r.lesson_id
            WHERE (@id IS NULL OR r.id=@id) ORDER BY l.title,r.title,r.id;
            """;
        command.Parameters.AddWithValue("@id", id);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var files = new List<ResourceFile>();
        while (await reader.ReadAsync(cancellationToken))
            files.Add(new(reader.GetInt64("id"), reader.GetInt64("lesson_id"), reader.GetString("lesson_title"),
                reader.GetString("title"), reader.GetString("description"), reader.GetString("resource_type"),
                reader.GetString("storage_key"), reader.GetString("original_filename"), reader.GetString("content_type"),
                reader.GetInt64("size_bytes"), reader.GetString("transcript"),
                reader.IsDBNull(reader.GetOrdinal("captions_storage_key")) ? null : reader.GetString("captions_storage_key")));
        return files;
    }

    public async Task SaveAsync(ResourceInput input, IFormFile? file, IFormFile? captions, CancellationToken cancellationToken)
    {
        var existing = input.Id > 0 ? (await ListAsync(input.Id, cancellationToken)).SingleOrDefault() : null;
        if (input.Id > 0 && existing is null) throw new ValidationException("Resource no longer exists.");
        if (file is null && existing is null) throw new ValidationException("Choose a file to upload.");
        if (file is null && existing?.Type != input.Type) throw new ValidationException("Upload a replacement file when changing the resource type.");
        var newKeys = new List<string>();
        var committed = false;
        try
        {
            (string Key, string Mime)? saved = file is null ? null : await StoreFileAsync(file, input.Type, cancellationToken);
            if (saved is not null) newKeys.Add(saved.Value.Key);
            var captionsKey = existing?.CaptionsKey;
            if (input.Type != "Video") captionsKey = null;
            if (captions is not null)
            {
                if (input.Type != "Video") throw new ValidationException("Captions are only used for video resources.");
                var savedCaptions = await StoreFileAsync(captions, "Captions", cancellationToken);
                newKeys.Add(savedCaptions.Key);
                captionsKey = savedCaptions.Key;
            }
            if (input.Type == "Video" && captionsKey is null) throw new ValidationException("Add a WebVTT caption file for the video.");
            await using var connection = await connections.OpenAsync(cancellationToken);
            await using var command = connection.CreateCommand();
            command.CommandText = input.Id == 0 ? """
                INSERT INTO resources (lesson_id,title,description,resource_type,storage_key,original_filename,content_type,size_bytes,transcript,captions_storage_key)
                VALUES (@lesson,@title,@description,@type,@key,@filename,@mime,@size,@transcript,@captions);
                """ : """
                UPDATE resources SET lesson_id=@lesson,title=@title,description=@description,resource_type=@type,
                    storage_key=@key,original_filename=@filename,content_type=@mime,size_bytes=@size,transcript=@transcript,captions_storage_key=@captions
                WHERE id=@id;
                """;
            command.Parameters.AddWithValue("@id", input.Id);
            command.Parameters.AddWithValue("@lesson", input.LessonId);
            command.Parameters.AddWithValue("@title", input.Title.Trim());
            command.Parameters.AddWithValue("@description", input.Description?.Trim() ?? "");
            command.Parameters.AddWithValue("@type", input.Type);
            command.Parameters.AddWithValue("@key", saved?.Key ?? existing!.StorageKey);
            command.Parameters.AddWithValue("@filename", file is null ? existing!.Filename : Path.GetFileName(file.FileName));
            command.Parameters.AddWithValue("@mime", saved?.Mime ?? existing!.ContentType);
            command.Parameters.AddWithValue("@size", file?.Length ?? existing!.Size);
            command.Parameters.AddWithValue("@transcript", input.Transcript.Trim());
            command.Parameters.AddWithValue("@captions", captionsKey);
            await command.ExecuteNonQueryAsync(cancellationToken);
            committed = true;
            if (existing is null) return;
            if (saved is not null) RemoveUpload(existing.StorageKey);
            if (existing.CaptionsKey is not null && existing.CaptionsKey != captionsKey) RemoveUpload(existing.CaptionsKey);
        }
        finally
        {
            if (!committed)
                foreach (var key in newKeys) RemoveUpload(key);
        }
    }

    public async Task DeleteAsync(long id, CancellationToken cancellationToken)
    {
        var file = (await ListAsync(id, cancellationToken)).SingleOrDefault();
        if (file is null) return;
        await using var connection = await connections.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM resources WHERE id=@id;";
        command.Parameters.AddWithValue("@id", id);
        await command.ExecuteNonQueryAsync(cancellationToken);
        RemoveUpload(file.StorageKey);
        if (file.CaptionsKey is not null) RemoveUpload(file.CaptionsKey);
    }

    private void RemoveUpload(string key)
    {
        if (!Regex.IsMatch(key, @"\A[a-zA-Z0-9_-]+\.[a-z0-9]+\z")) return;
        File.Delete(Path.Combine(UploadDirectory, key));
    }

    private async Task<(string Key, string Mime)> StoreFileAsync(IFormFile file, string type, CancellationToken cancellationToken)
    {
        var limit = type == "Video" ? 50 * 1024 * 1024 : type == "Captions" ? 256 * 1024 : 10 * 1024 * 1024;
        if (file.Length == 0 || file.Length > limit) throw new ValidationException($"Choose a non-empty file up to {limit / 1024} KiB.");
        if (Path.GetFileName(file.FileName).Length > 255) throw new ValidationException("The filename is too long.");
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        await using var source = file.OpenReadStream();
        var header = new byte[12];
        var count = await source.ReadAsync(header, cancellationToken);
        var valid = type switch
        {
            "Pdf" => extension == ".pdf" && count >= 5 && header.AsSpan(0, 5).SequenceEqual("%PDF-"u8),
            "Video" => extension == ".mp4" && count >= 12 && header.AsSpan(4, 4).SequenceEqual("ftyp"u8),
            "Audio" => extension == ".mp3" && count >= 3 && (header.AsSpan(0, 3).SequenceEqual("ID3"u8) || (header[0] == 255 && (header[1] & 224) == 224))
                || extension == ".wav" && count >= 12 && header.AsSpan(0, 4).SequenceEqual("RIFF"u8) && header.AsSpan(8, 4).SequenceEqual("WAVE"u8),
            "Captions" => extension == ".vtt",
            _ => false
        };
        if (!valid) throw new ValidationException("File contents do not match the selected type. Use PDF, MP4, MP3, WAV or WebVTT captions.");
        source.Position = 0;
        if (type == "Captions")
        {
            using var reader = new StreamReader(source, new UTF8Encoding(false, true), leaveOpen: true);
            string text;
            try { text = await reader.ReadToEndAsync(cancellationToken); }
            catch (DecoderFallbackException) { throw new ValidationException("Captions must use UTF-8 text."); }
            if (!text.StartsWith("WEBVTT", StringComparison.Ordinal) || !text.Contains("-->")) throw new ValidationException("Use a WebVTT file with timed captions.");
            source.Position = 0;
        }
        var key = $"{Guid.NewGuid():N}{extension}";
        Directory.CreateDirectory(UploadDirectory);
        var path = Path.Combine(UploadDirectory, key);
        var complete = false;
        try
        {
            await using var target = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, true);
            await source.CopyToAsync(target, cancellationToken);
            complete = true;
        }
        finally { if (!complete) File.Delete(path); }
        return (key, extension switch { ".pdf" => "application/pdf", ".mp4" => "video/mp4", ".mp3" => "audio/mpeg", ".wav" => "audio/wav", _ => "text/vtt" });
    }
}
