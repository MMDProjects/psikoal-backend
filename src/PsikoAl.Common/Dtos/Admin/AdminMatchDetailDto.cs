namespace PsikoAl.Common.Dtos.Admin;

public sealed record AdminMatchDetailDto(
    Guid Id,
    string ClientFullName,
    string ClientEmail,
    string ExpertFullName,
    string ListingTitle,
    decimal OfferPrice,
    string Status,
    DateTimeOffset? ClientReleasedAt,
    DateTimeOffset? ExpertReleasedAt,
    bool ReleasedByAdmin,
    string? ReleaseReason,
    DateTimeOffset CreatedAt);
