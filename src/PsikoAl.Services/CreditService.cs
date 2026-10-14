using Microsoft.EntityFrameworkCore;
using PsikoAl.Common.Constants;
using PsikoAl.Common.Dtos.Credit;
using PsikoAl.Common.Presentation;
using PsikoAl.Data.Repositories.Abstractions;
using PsikoAl.Services.Abstractions;
using PsikoAl.Services.Mapping;

namespace PsikoAl.Services;

public sealed class CreditService(IUnitOfWork unitOfWork, TimeProvider timeProvider) : ICreditService
{
    public async Task<CreditBalanceDto> GetMyBalanceAsync(Guid expertUserId, CancellationToken cancellationToken)
    {
        var balance = await GetRequiredBalanceAsync(expertUserId, cancellationToken);
        var offerCost = await OfferCreditCost.ReadAsync(unitOfWork.SystemSettings, cancellationToken);
        return new CreditBalanceDto(balance, offerCost, CreditLabelFormatter.Balance(balance));
    }

    public async Task<CreditTransactionListResult> ListMyTransactionsAsync(
        Guid expertUserId,
        int? page,
        int? perPage,
        CancellationToken cancellationToken)
    {
        await GetRequiredBalanceAsync(expertUserId, cancellationToken);

        var effectivePage = Math.Max(1, page ?? 1);
        var effectivePerPage = Math.Clamp(
            perPage ?? CreditFieldLimits.TransactionsDefaultPerPage,
            1,
            CreditFieldLimits.TransactionsMaxPerPage);

        var query = unitOfWork.CreditTransactions.QueryForExpert(expertUserId);
        var total = await query.CountAsync(cancellationToken);
        var rows = await query
            .Skip((effectivePage - 1) * effectivePerPage)
            .Take(effectivePerPage)
            .Select(transaction => new CreditTransactionReadModel(
                transaction.Id,
                transaction.Amount,
                transaction.BalanceAfter,
                transaction.Type,
                transaction.OfferId,
                transaction.Reason,
                transaction.Payment != null && transaction.Payment.Package != null ? transaction.Payment.Package.Name : null,
                transaction.Offer != null && transaction.Offer.Listing != null ? transaction.Offer.Listing.Title : null,
                transaction.CreatedAt))
            .ToListAsync(cancellationToken);

        var now = timeProvider.GetUtcNow();
        var dtos = rows.Select(row => CreditMapper.ToTransactionDto(row, now)).ToList();
        return new CreditTransactionListResult(dtos, effectivePage, effectivePerPage, total);
    }

    public async Task<IReadOnlyList<CreditPackageDto>> ListActivePackagesAsync(CancellationToken cancellationToken)
    {
        var packages = await unitOfWork.CreditPackages.ListActiveAsync(cancellationToken);
        return packages.Select(CreditMapper.ToPackageDto).ToList();
    }

    private async Task<int> GetRequiredBalanceAsync(Guid expertUserId, CancellationToken cancellationToken)
        => await unitOfWork.ExpertCredits.GetBalanceAsync(expertUserId, cancellationToken)
            ?? throw await ExpertAccess.MissingExpertErrorAsync(
                unitOfWork, expertUserId, ErrorKeys.ExpertRoleRequired, cancellationToken);
}
