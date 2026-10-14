using Microsoft.EntityFrameworkCore;
using PsikoAl.Common.Constants;
using PsikoAl.Data.Entities;
using PsikoAl.Data.Repositories.Abstractions;

namespace PsikoAl.Data.Repositories;

public sealed class CreditPaymentRepository(AppDbContext dbContext)
    : Repository<CreditPayment, Guid>(dbContext), ICreditPaymentRepository
{
    public Task<CreditPayment?> GetByMerchantOidAsync(string merchantOid, CancellationToken cancellationToken)
        => DbContext.CreditPayments
            .AsNoTracking()
            .FirstOrDefaultAsync(payment => payment.MerchantOid == merchantOid, cancellationToken);

    public Task<bool> AnyForPackageAsync(Guid packageId, CancellationToken cancellationToken)
        => DbContext.CreditPayments.AnyAsync(payment => payment.PackageId == packageId, cancellationToken);

    public async Task<bool> TryCompleteAsync(
        Guid paymentId,
        string status,
        string? providerPayload,
        DateTimeOffset completedAt,
        CancellationToken cancellationToken)
        => await DbContext.CreditPayments
            .Where(payment => payment.Id == paymentId && payment.Status == CreditPaymentStatuses.Pending)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(payment => payment.Status, status)
                    .SetProperty(payment => payment.ProviderPayload, providerPayload)
                    .SetProperty(payment => payment.CompletedAt, completedAt),
                cancellationToken) == 1;
}
