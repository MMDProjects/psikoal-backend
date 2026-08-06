namespace PsikoAl.Common.Dtos.Offer;

public sealed record OfferExpertDto(
    Guid Id,
    string Name,
    string Title,
    string? Initials,
    string? AvatarUrl,
    double Rating);
