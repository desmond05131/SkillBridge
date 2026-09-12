namespace SkillBridge.Web.Features.Lessons;

public sealed record LessonSummary(long Id, long CourseId, string CourseTitle, string Title, int SequenceNumber, int DurationMinutes, bool IsPublished);

public sealed record LessonResource(string Title, string Description, string ResourceType, long SizeBytes);

public sealed record LessonDetail(LessonSummary Lesson, string Summary, string Body, IReadOnlyList<LessonResource> Resources);

public interface ILessonReader
{
    Task<LessonDetail?> GetByIdAsync(long id, long userId, bool isAdmin, CancellationToken cancellationToken);
    Task<IReadOnlyList<LessonSummary>> GetForAdministrationAsync(CancellationToken cancellationToken);
}
