namespace PsikoAl.Common.Dtos.Assessment;

public sealed record MyAssessmentResultDto(
    Guid Id,
    int Score,
    string Level,
    string Summary,
    IReadOnlyList<string> Suggestions,
    DateTimeOffset CreatedAt,
    Guid AssessmentId,
    string AssessmentTitle);
