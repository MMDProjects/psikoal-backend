namespace PsikoAl.Common.Dtos.Offer.Create;

public sealed record CreateOfferDto(
    Guid ListingId,
    string? Title,
    decimal Price,
    string SessionType,
    string? Description);
