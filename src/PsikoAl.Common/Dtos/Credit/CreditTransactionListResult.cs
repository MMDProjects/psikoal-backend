namespace PsikoAl.Common.Dtos.Credit;

public sealed record CreditTransactionListResult(IReadOnlyList<CreditTransactionDto> Data, int Page, int PerPage, int Total);
