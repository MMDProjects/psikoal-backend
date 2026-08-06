namespace PsikoAl.Common.Dtos.Assessment;

public sealed record AssessmentResultDto(
    Guid Id,
    int Score,
    string Level,
    string Summary,
    IReadOnlyList<string> Suggestions,
    DateTimeOffset CreatedAt);
