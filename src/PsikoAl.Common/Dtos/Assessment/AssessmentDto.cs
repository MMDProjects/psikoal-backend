namespace PsikoAl.Common.Dtos.Assessment;

public sealed record AssessmentDto(
    Guid Id,
    string Title,
    string Description,
    IReadOnlyList<QuestionDto> Questions,
    int EstimatedMinutes);
