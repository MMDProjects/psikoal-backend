namespace PsikoAl.Common.Dtos.Admin;

public sealed record AdminCreditPackageDto(
    Guid Id,
    string Name,
    int Credits,
    decimal PriceTry,
    bool IsActive,
    int SortOrder,
    int PaidPurchaseCount,
    DateTimeOffset UpdatedAt);
