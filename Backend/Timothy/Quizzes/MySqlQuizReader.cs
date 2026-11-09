using SkillBridge.Web.Features.Quizzes;
using SkillBridge.Web.Infrastructure.Data;

namespace SkillBridge.Web.Infrastructure.Quizzes;

public sealed class MySqlQuizReader(MySqlConnectionFactory connectionFactory) : IQuizReader
{
    public async Task<QuizOverview?> GetOverviewAsync(long quizId, long userId, bool canPreviewDraft, CancellationToken cancellationToken)
    {
        await using var connection = await connectionFactory.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT q.id, q.course_id, c.title AS course_title, q.title, q.instructions,
                   (SELECT COUNT(*) FROM questions question WHERE question.quiz_id = q.id) AS question_count,q.pass_percentage, q.is_published, c.is_published AS is_course_published
            FROM quizzes q
            INNER JOIN courses c ON c.id = q.course_id
            WHERE q.id = @quizId
              AND (@canPreviewDraft = TRUE OR (
                  q.is_published = TRUE AND c.is_published = TRUE
                  AND EXISTS (
                      SELECT 1 FROM enrolments e
                      WHERE e.course_id = q.course_id AND e.user_id = @userId
                  )
              ));
            """;
        command.Parameters.AddWithValue("@quizId", quizId);
        command.Parameters.AddWithValue("@userId", userId);
        command.Parameters.AddWithValue("@canPreviewDraft", canPreviewDraft);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken)) return null;

        return new QuizOverview(
            reader.GetInt64("id"), reader.GetInt64("course_id"), reader.GetString("course_title"),
            reader.GetString("title"), reader.GetString("instructions"), reader.GetInt32("question_count"),
            reader.GetDecimal("pass_percentage"), reader.GetBoolean("is_published"), reader.GetBoolean("is_course_published"));
    }

    public async Task<IReadOnlyList<QuizSummary>> GetAllAsync(CancellationToken cancellationToken, string? search = null, string? status = null)
    { //this reads entire code for databasing
        await using var connection = await connectionFactory.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT q.id, c.title AS course_title, q.title,q.pass_percentage,
            q.is_published, (SELECT COUNT(*) FROM questions question WHERE question.quiz_id = q.id) AS question_count,
            (SELECT COUNT(*) FROM attempts qa WHERE qa.quiz_id = q.id) AS attempt_count
            FROM quizzes q
            INNER JOIN courses c ON c.id = q.course_id
            WHERE (@search = '' OR LOCATE(@search,q.title)>0 OR LOCATE(@search,c.title)>0)
              AND (@status = '' OR (@status='Draft' AND q.is_published=FALSE) OR (@status='Published' AND q.is_published=TRUE))
            ORDER BY c.title, q.title, q.id;
            """; // to add On
        command.Parameters.AddWithValue("@search", search?.Trim() ?? "");
        command.Parameters.AddWithValue("@status", status ?? "");
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var quizzes = new List<QuizSummary>();
        while (await reader.ReadAsync(cancellationToken))
        {
            quizzes.Add(new QuizSummary(reader.GetInt64("id"), reader.GetString("course_title"), reader.GetString("title"), reader.GetDecimal("pass_percentage"), reader.GetBoolean("is_published"), reader.GetInt32("question_count"),
            reader.GetInt32("attempt_count")));
        }

        return quizzes;

    }


}
