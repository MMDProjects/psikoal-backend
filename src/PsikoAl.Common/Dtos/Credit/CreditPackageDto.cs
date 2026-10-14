namespace PsikoAl.Common.Dtos.Credit;

public sealed record CreditPackageDto(
    Guid Id,
    string Name,
    int Credits,
    decimal PriceTry,
    string PriceLabel,
    string UnitPriceLabel);
