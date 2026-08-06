namespace PsikoAl.Common.Dtos.Match;

public sealed record MatchOfferDto(
    Guid Id,
    string? Title,
    decimal Price,
    string Description,
    string SessionType,
    string Status);
