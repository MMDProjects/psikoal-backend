namespace PsikoAl.Common.Dtos.Admin;

public sealed record AdminCreditTransactionDto(
    Guid Id,
    int Amount,
    int BalanceAfter,
    string Type,
    string TypeLabel,
    string? Reason,
    Guid? OfferId,
    Guid? PaymentId,
    string? AdminName,
    DateTimeOffset CreatedAt);
