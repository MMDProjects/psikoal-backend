namespace PsikoAl.Common.Dtos.Assessment;

public sealed record AssessmentListItemDto(
    Guid Id,
    string Title,
    string Category,
    int EstimatedMinutes,
    int QuestionCount);
