using PsikoAl.Common.Dtos.Credit;

namespace PsikoAl.Services.Abstractions;

public interface ICreditService
{
    Task<CreditBalanceDto> GetMyBalanceAsync(Guid expertUserId, CancellationToken cancellationToken);

    Task<CreditTransactionListResult> ListMyTransactionsAsync(
        Guid expertUserId,
        int? page,
        int? perPage,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<CreditPackageDto>> ListActivePackagesAsync(CancellationToken cancellationToken);
}
