using MySqlConnector;
using SkillBridge.Web.Infrastructure.Data;

namespace SkillBridge.Web.Features.Quizzes;

public sealed record QuizOption(long Id, string Text, bool IsCorrect);
public sealed record QuizQuestion(long Id, int SequenceNumber, string Text, string Explanation, int Marks, List<QuizOption> Options);
public sealed record SavedAttempt(long Id, long QuizId, bool IsSubmitted, int Score, int MaximumMarks, DateTime? SubmittedAt);
public sealed record AttemptHistory(long Id, int Number, int Score, int MaximumMarks, DateTime SubmittedAt, bool IsPassed);

public sealed class QuizAttemptStore(MySqlConnectionFactory connections)
{
    public async Task<List<QuizQuestion>> QuestionsAsync(long quizId, CancellationToken cancellationToken)
    {
        await using var connection = await connections.OpenAsync(cancellationToken);
        return await ReadQuestionsAsync(connection, null, quizId, cancellationToken);
    }

    private static async Task<List<QuizQuestion>> ReadQuestionsAsync(MySqlConnection connection, MySqlTransaction? transaction, long quizId, CancellationToken cancellationToken)
    {
        await using var command = new MySqlCommand("""
            SELECT q.id, q.sequence_number, q.question_text, q.explanation, q.marks,
                   o.id AS option_id, o.option_text, o.is_correct
            FROM questions q LEFT JOIN options o ON o.question_id=q.id
            WHERE q.quiz_id=@quizId ORDER BY q.sequence_number, o.sequence_number;
            """, connection, transaction);
        command.Parameters.AddWithValue("@quizId", quizId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var questions = new List<QuizQuestion>();
        while (await reader.ReadAsync(cancellationToken))
        {
            var id = reader.GetInt64("id");
            if (questions.Count == 0 || questions[^1].Id != id)
                questions.Add(new(id, reader.GetInt32("sequence_number"), reader.GetString("question_text"), reader.GetString("explanation"), reader.GetInt32("marks"), []));
            if (reader.IsDBNull(reader.GetOrdinal("option_id"))) continue;
            questions[^1].Options.Add(new(reader.GetInt64("option_id"), reader.GetString("option_text"), reader.GetBoolean("is_correct")));
        }
        return questions;
    }

    public static bool IsReady(IReadOnlyList<QuizQuestion> questions) => questions.Count > 0
        && questions.All(question => question.Options.Count == 4 && question.Options.Count(option => option.IsCorrect) == 1);

    public async Task<long> StartAsync(long quizId, long userId, CancellationToken cancellationToken)
    {
        await using var connection = await connections.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        await using var command = new MySqlCommand("SELECT id FROM quizzes WHERE id=@quizId FOR UPDATE;", connection, transaction);
        command.Parameters.AddWithValue("@quizId", quizId);
        if (await command.ExecuteScalarAsync(cancellationToken) is null) throw new System.ComponentModel.DataAnnotations.ValidationException("Quiz no longer exists.");
        if (!IsReady(await ReadQuestionsAsync(connection, transaction, quizId, cancellationToken)))
            throw new System.ComponentModel.DataAnnotations.ValidationException("The quiz is being updated. Please try again.");
        command.CommandText = "INSERT INTO attempts (quiz_id,user_id) VALUES (@quizId,@userId) ON DUPLICATE KEY UPDATE id=LAST_INSERT_ID(id); SELECT LAST_INSERT_ID();";
        command.Parameters.AddWithValue("@userId", userId);
        var id = Convert.ToInt64(await command.ExecuteScalarAsync(cancellationToken));
        await transaction.CommitAsync(cancellationToken);
        return id;
    }

    public async Task<SavedAttempt?> FindAsync(long attemptId, long userId, CancellationToken cancellationToken)
    {
        await using var connection = await connections.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT * FROM attempts WHERE id=@id AND user_id=@userId;";
        command.Parameters.AddWithValue("@id", attemptId);
        command.Parameters.AddWithValue("@userId", userId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken)) return null;
        var submitted = reader.GetString("status") == "Submitted";
        return new(attemptId, reader.GetInt64("quiz_id"), submitted,
            submitted ? reader.GetInt32("earned_marks") : 0,
            submitted ? reader.GetInt32("maximum_marks") : 0,
            submitted ? reader.GetDateTime("submitted_at_utc") : null);
    }

    public async Task<Dictionary<long, long>> AnswersAsync(long attemptId, CancellationToken cancellationToken)
    {
        await using var connection = await connections.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT question_id,selected_option_id FROM answers WHERE attempt_id=@id;";
        command.Parameters.AddWithValue("@id", attemptId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var answers = new Dictionary<long, long>();
        while (await reader.ReadAsync(cancellationToken))
            answers.Add(reader.GetInt64("question_id"), reader.GetInt64("selected_option_id"));
        return answers;
    }

    public async Task<List<AttemptHistory>> HistoryAsync(long quizId, long userId, CancellationToken cancellationToken)
    {
        await using var connection = await connections.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT a.id, ROW_NUMBER() OVER (ORDER BY a.started_at_utc,a.id) AS number,
                   a.earned_marks,a.maximum_marks,a.submitted_at_utc,
                   (a.earned_marks * 100 >= a.maximum_marks * q.pass_percentage) AS passed
            FROM attempts a JOIN quizzes q ON q.id=a.quiz_id
            WHERE a.quiz_id=@quizId AND a.user_id=@userId AND a.status='Submitted'
            ORDER BY a.started_at_utc DESC,a.id DESC;
            """;
        command.Parameters.AddWithValue("@quizId", quizId);
        command.Parameters.AddWithValue("@userId", userId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var history = new List<AttemptHistory>();
        while (await reader.ReadAsync(cancellationToken))
            history.Add(new(reader.GetInt64("id"), reader.GetInt32("number"), reader.GetInt32("earned_marks"), reader.GetInt32("maximum_marks"), reader.GetDateTime("submitted_at_utc"), reader.GetBoolean("passed")));
        return history;
    }

    public async Task<bool> SubmitAsync(long attemptId, long userId, IReadOnlyDictionary<long, long> selections, CancellationToken cancellationToken)
    {
        await using var connection = await connections.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "SELECT quiz_id,status FROM attempts WHERE id=@id AND user_id=@userId FOR UPDATE;";
        command.Parameters.AddWithValue("@id", attemptId);
        command.Parameters.AddWithValue("@userId", userId);
        long quizId;
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            if (!await reader.ReadAsync(cancellationToken)) return false;
            if (reader.GetString("status") == "Submitted") return true;
            quizId = reader.GetInt64("quiz_id");
        }
        var questions = await ReadQuestionsAsync(connection, transaction, quizId, cancellationToken);
        if (!IsReady(questions) || selections.Count != questions.Count) return false;
        if (questions.Any(question => !selections.TryGetValue(question.Id, out var selected)
            || !question.Options.Any(option => option.Id == selected))) return false;

        var score = 0;
        foreach (var question in questions)
        {
            var selected = selections[question.Id];
            var marks = question.Options.Single(option => option.Id == selected).IsCorrect ? question.Marks : 0;
            score += marks;
            command.Parameters.Clear();
            command.CommandText = """
                INSERT INTO answers (attempt_id,quiz_id,question_id,selected_option_id,earned_marks)
                VALUES (@id,@quizId,@questionId,@optionId,@marks)
                ON DUPLICATE KEY UPDATE selected_option_id=@optionId,earned_marks=@marks;
                """;
            command.Parameters.AddWithValue("@id", attemptId);
            command.Parameters.AddWithValue("@quizId", quizId);
            command.Parameters.AddWithValue("@questionId", question.Id);
            command.Parameters.AddWithValue("@optionId", selected);
            command.Parameters.AddWithValue("@marks", marks);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
        command.Parameters.Clear();
        command.CommandText = "UPDATE attempts SET status='Submitted',earned_marks=@score,maximum_marks=@maximum,submitted_at_utc=UTC_TIMESTAMP() WHERE id=@id;";
        command.Parameters.AddWithValue("@id", attemptId);
        command.Parameters.AddWithValue("@score", score);
        command.Parameters.AddWithValue("@maximum", questions.Sum(question => question.Marks));
        await command.ExecuteNonQueryAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }
}
