namespace PsikoAl.Common.Dtos.Admin;

public sealed record AdminAssessmentListItemDto(
    Guid Id,
    string Title,
    string Category,
    int EstimatedMinutes,
    int QuestionCount,
    bool IsActive,
    int SortOrder);
