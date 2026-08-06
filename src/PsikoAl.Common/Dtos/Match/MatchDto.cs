namespace PsikoAl.Common.Dtos.Match;

public sealed record MatchDto(
    Guid Id,
    Guid ListingId,
    Guid AcceptedOfferId,
    Guid ClientId,
    Guid ExpertId,
    string Status,
    DateTimeOffset CreatedAt,
    string CreatedAtRelative,
    MatchClientDto Client,
    MatchExpertDto? Expert,
    MatchListingDto? Listing,
    MatchOfferDto? Offer,
    DateTimeOffset? ClientReleasedAt,
    DateTimeOffset? ExpertReleasedAt);
