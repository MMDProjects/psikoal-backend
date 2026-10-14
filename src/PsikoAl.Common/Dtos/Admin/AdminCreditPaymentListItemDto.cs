namespace PsikoAl.Common.Dtos.Admin;

public sealed record AdminCreditPaymentListItemDto(
    Guid Id,
    string MerchantOid,
    Guid ExpertId,
    string ExpertName,
    string ExpertEmail,
    string PackageName,
    int Credits,
    decimal AmountTry,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? CompletedAt);
