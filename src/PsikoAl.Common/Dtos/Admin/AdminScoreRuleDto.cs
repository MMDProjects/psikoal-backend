namespace PsikoAl.Common.Dtos.Admin;

public sealed record AdminScoreRuleDto(
    Guid Id,
    int MinScore,
    int MaxScore,
    string Level,
    string Summary,
    IReadOnlyList<string> Suggestions);
