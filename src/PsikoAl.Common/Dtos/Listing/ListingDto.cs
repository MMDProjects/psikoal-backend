namespace PsikoAl.Common.Dtos.Listing;

public sealed record ListingDto(
    Guid Id,
    Guid ClientId,
    ListingClientDto Client,
    string ClientDisplayName,
    string Title,
    string Description,
    IReadOnlyList<string> Specialization,
    decimal BudgetMin,
    decimal BudgetMax,
    string PreferredSessionType,
    string? City,
    int OfferCount,
    string Status,
    string BudgetLabel,
    DateTimeOffset? ExpiresAt,
    DateTimeOffset CreatedAt,
    string CreatedAtRelative,
    bool ViewerHasOffered,
    Guid? ViewerOfferId,
    string? RejectionReason);
