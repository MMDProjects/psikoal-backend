namespace PsikoAl.Common.Dtos.Admin;

public sealed record AdminMatchListItemDto(
    Guid Id,
    string ClientFullName,
    string ExpertFullName,
    string ListingTitle,
    string Status,
    DateTimeOffset? ClientReleasedAt,
    DateTimeOffset? ExpertReleasedAt,
    DateTimeOffset CreatedAt);
