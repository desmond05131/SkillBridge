using System.ComponentModel.DataAnnotations;
using MySqlConnector;
using SkillBridge.Web.Infrastructure.Data;

namespace SkillBridge.Web.Features.Quizzes;

public sealed class QuestionInput
{
    public long Id { get; set; }
    [Range(1, long.MaxValue)] public long QuizId { get; set; }
    [Required, StringLength(1000, MinimumLength = 5)] public string Text { get; set; } = "";
    [Required, StringLength(1500)] public string Explanation { get; set; } = "";
    [Range(1, 100)] public int Marks { get; set; } = 1;
    [Range(1, 10000)] public int SequenceNumber { get; set; } = 1;
    [Required, Range(0, 3)] public int? CorrectOption { get; set; }
    public string[] Options { get; set; } = ["", "", "", ""];
}

public sealed record QuestionQuiz(long Id, string Title, bool Published, bool HasAttempts);

public sealed class QuestionStore(MySqlConnectionFactory connections)
{
    public async Task<QuestionQuiz?> QuizAsync(long id, CancellationToken cancellationToken)
    {
        await using var connection = await connections.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT id,title,is_published,EXISTS(SELECT 1 FROM attempts WHERE quiz_id=@id) AS attempted FROM quizzes WHERE id=@id;";
        command.Parameters.AddWithValue("@id", id);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken)) return null;
        return new(id, reader.GetString("title"), reader.GetBoolean("is_published"), reader.GetBoolean("attempted"));
    }

    public async Task SaveAsync(QuestionInput input, CancellationToken cancellationToken)
    {
        await using var connection = await connections.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        await EnsureEditableAsync(connection, transaction, input.QuizId, cancellationToken);
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        if (input.Id > 0)
        {
            command.CommandText = "SELECT COUNT(*) FROM questions WHERE id=@id AND quiz_id=@quiz;";
            command.Parameters.AddWithValue("@id", input.Id);
            command.Parameters.AddWithValue("@quiz", input.QuizId);
            if (Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken)) != 1) throw new ValidationException("Question no longer exists in this quiz.");
        }
        command.Parameters.Clear();
        command.CommandText = input.Id == 0 ? """
            INSERT INTO questions (quiz_id,question_text,explanation,marks,sequence_number) VALUES (@quiz,@text,@explanation,@marks,@sequence);
            """ : """
            UPDATE questions SET question_text=@text,explanation=@explanation,marks=@marks,sequence_number=@sequence WHERE id=@id AND quiz_id=@quiz;
            """;
        command.Parameters.AddWithValue("@quiz", input.QuizId);
        command.Parameters.AddWithValue("@id", input.Id);
        command.Parameters.AddWithValue("@text", input.Text.Trim());
        command.Parameters.AddWithValue("@explanation", input.Explanation.Trim());
        command.Parameters.AddWithValue("@marks", input.Marks);
        command.Parameters.AddWithValue("@sequence", input.SequenceNumber);
        await command.ExecuteNonQueryAsync(cancellationToken);
        var questionId = input.Id == 0 ? command.LastInsertedId : input.Id;
        command.Parameters.Clear();
        command.CommandText = "DELETE FROM options WHERE question_id=@id;";
        command.Parameters.AddWithValue("@id", questionId);
        await command.ExecuteNonQueryAsync(cancellationToken);
        foreach (var (text, index) in input.Options.Select((text, index) => (text, index)))
        {
            command.Parameters.Clear();
            command.CommandText = "INSERT INTO options (question_id,option_text,sequence_number,is_correct) VALUES (@id,@text,@sequence,@correct);";
            command.Parameters.AddWithValue("@id", questionId);
            command.Parameters.AddWithValue("@text", text.Trim());
            command.Parameters.AddWithValue("@sequence", index + 1);
            command.Parameters.AddWithValue("@correct", index == input.CorrectOption);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task DeleteAsync(long quizId, long id, CancellationToken cancellationToken)
    {
        await using var connection = await connections.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        await EnsureEditableAsync(connection, transaction, quizId, cancellationToken);
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "DELETE o FROM options o JOIN questions q ON q.id=o.question_id WHERE q.id=@id AND q.quiz_id=@quiz; DELETE FROM questions WHERE id=@id AND quiz_id=@quiz;";
        command.Parameters.AddWithValue("@id", id);
        command.Parameters.AddWithValue("@quiz", quizId);
        await command.ExecuteNonQueryAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    private static async Task EnsureEditableAsync(MySqlConnection connection, MySqlTransaction transaction, long id, CancellationToken cancellationToken)
    {
        await using var command = new MySqlCommand("SELECT is_published FROM quizzes WHERE id=@id FOR UPDATE;", connection, transaction);
        command.Parameters.AddWithValue("@id", id);
        var published = await command.ExecuteScalarAsync(cancellationToken);
        if (published is null) throw new ValidationException("Quiz not found.");
        if (Convert.ToBoolean(published)) throw new ValidationException("Set the quiz to Draft before changing questions.");
        command.CommandText = "SELECT COUNT(*) FROM attempts WHERE quiz_id=@id;";
        if (Convert.ToInt64(await command.ExecuteScalarAsync(cancellationToken)) > 0) throw new ValidationException("Questions cannot change after the first attempt starts.");
    }
}
