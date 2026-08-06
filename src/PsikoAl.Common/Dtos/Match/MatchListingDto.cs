namespace PsikoAl.Common.Dtos.Match;

public sealed record MatchListingDto(
    Guid Id,
    string Title,
    string? Description,
    IReadOnlyList<string> Specialization,
    decimal BudgetMin,
    decimal BudgetMax,
    string? BudgetLabel,
    string PreferredSessionType,
    string Status,
    string? City);
