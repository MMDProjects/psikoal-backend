namespace PsikoAl.Common.Dtos.Credit;

public sealed record CreditTransactionDto(
    Guid Id,
    int Amount,
    int BalanceAfter,
    string Type,
    string TypeLabel,
    string Description,
    Guid? OfferId,
    DateTimeOffset CreatedAt,
    string TimeLabel);
