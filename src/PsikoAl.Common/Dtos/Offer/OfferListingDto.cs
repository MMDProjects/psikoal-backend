namespace PsikoAl.Common.Dtos.Offer;

public sealed record OfferListingDto(
    Guid Id,
    string Title,
    Guid ClientId,
    string? City,
    string ClientDisplayName,
    OfferListingClientDto? Client);
