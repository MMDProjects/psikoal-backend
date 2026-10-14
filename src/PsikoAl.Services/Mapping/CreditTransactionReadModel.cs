namespace PsikoAl.Services.Mapping;

public sealed record CreditTransactionReadModel(
    Guid Id,
    int Amount,
    int BalanceAfter,
    string Type,
    Guid? OfferId,
    string? Reason,
    string? PackageName,
    string? ListingTitle,
    DateTimeOffset CreatedAt);
