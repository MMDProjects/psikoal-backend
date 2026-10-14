using PsikoAl.Data.Entities;

namespace PsikoAl.Data.Repositories.Abstractions;

public interface ICreditPaymentRepository : IRepository<CreditPayment, Guid>
{
    Task<CreditPayment?> GetByMerchantOidAsync(string merchantOid, CancellationToken cancellationToken);

    Task<bool> AnyForPackageAsync(Guid packageId, CancellationToken cancellationToken);

    // Yalnız PENDING bir ödemeyi sonuçlandırır. Aynı bildirim iki kez (ya da eşzamanlı)
    // gelirse ikinci çağrı false döner: kredi bir kez yüklenir.
    Task<bool> TryCompleteAsync(
        Guid paymentId,
        string status,
        string? providerPayload,
        DateTimeOffset completedAt,
        CancellationToken cancellationToken);
}
