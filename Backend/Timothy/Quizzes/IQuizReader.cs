namespace SkillBridge.Web.Features.Quizzes;

public interface IQuizReader
{
    Task<QuizOverview?> GetOverviewAsync(long quizId, long userId, bool canPreviewDraft, CancellationToken cancellationToken);
    Task<IReadOnlyList<QuizSummary>> GetAllAsync(CancellationToken cancellationToken, string? search = null, string? status = null);

    // update create delete in here this is for all the quiz necessities

}
