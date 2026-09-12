using SkillBridge.Web.Features.Administration;
using SkillBridge.Web.Infrastructure.Data;

namespace SkillBridge.Web.Infrastructure.Administration;

public sealed class MySqlDashboardReader(MySqlConnectionFactory connectionFactory) : IDashboardReader
{
    public async Task<DashboardSummary> GetSummaryAsync(CancellationToken cancellationToken)
    {
        await using var connection = await connectionFactory.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT (SELECT COUNT(*) FROM users u INNER JOIN roles r ON r.id = u.role_id WHERE r.name = 'Member') AS members,
                   (SELECT COUNT(*) FROM courses WHERE is_published = TRUE) AS published_courses,
                   (SELECT COUNT(*) FROM courses WHERE is_published = FALSE) AS draft_courses,
                   (SELECT COUNT(*) FROM lessons) AS lessons;
            """;
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        await reader.ReadAsync(cancellationToken);
        return new DashboardSummary(reader.GetInt32("members"), reader.GetInt32("published_courses"), reader.GetInt32("draft_courses"), reader.GetInt32("lessons"));
    }
}
