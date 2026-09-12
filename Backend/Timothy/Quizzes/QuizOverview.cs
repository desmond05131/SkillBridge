namespace SkillBridge.Web.Features.Quizzes;

public sealed record QuizOverview(
    long Id,
    long CourseId,
    string CourseTitle,
    string Title,
    string Instructions,
    int QuestionCount,
    decimal PassPercentage,
    bool IsPublished,
    bool IsCoursePublished);

public sealed record QuizSummary(long Id, string CourseTitle, string Title, bool IsPublished, int QuestionCount);
