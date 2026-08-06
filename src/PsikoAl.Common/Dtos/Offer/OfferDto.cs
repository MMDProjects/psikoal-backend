namespace PsikoAl.Common.Dtos.Offer;

public sealed record OfferDto(
    Guid Id,
    Guid ListingId,
    Guid ExpertId,
    string? Title,
    decimal Price,
    string Description,
    string SessionType,
    string Status,
    Guid? MatchId,
    DateTimeOffset CreatedAt,
    string CreatedAtRelative,
    OfferListingDto? Listing,
    OfferExpertDto? Expert);
