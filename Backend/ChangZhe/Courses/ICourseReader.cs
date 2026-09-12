namespace SkillBridge.Web.Features.Courses;

public interface ICourseReader
{
    Task<IReadOnlyList<CourseSummary>> GetPublishedAsync(string? search, CancellationToken cancellationToken);
    Task<CourseDetail?> GetByIdAsync(long id, bool includeDraft, CancellationToken cancellationToken);
    Task<IReadOnlyList<CourseSummary>> GetEnrolledAsync(long userId, CancellationToken cancellationToken);
}
