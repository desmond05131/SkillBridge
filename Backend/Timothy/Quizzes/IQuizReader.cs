namespace SkillBridge.Web.Features.Quizzes;

public interface IQuizReader
{
    Task<QuizOverview?> GetOverviewAsync(long quizId, long userId, bool canPreviewDraft, CancellationToken cancellationToken);
    Task<IReadOnlyList<QuizSummary>> GetAllAsync(CancellationToken cancellationToken);
}
