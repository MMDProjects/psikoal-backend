namespace PsikoAl.Common.Dtos.Match;

public sealed record MatchClientDto(
    Guid Id,
    string FullName,
    string? Initials,
    string? Email,
    string? Phone,
    DateTimeOffset? CreatedAt);
