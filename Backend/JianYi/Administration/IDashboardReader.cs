namespace SkillBridge.Web.Features.Administration;

public sealed record DashboardSummary(int Members, int PublishedCourses, int DraftCourses, int Lessons);

public interface IDashboardReader
{
    Task<DashboardSummary> GetSummaryAsync(CancellationToken cancellationToken);
}
