namespace PsikoAl.Data.Repositories.Abstractions;

public interface IExpertCreditRepository
{
    Task<int?> GetBalanceAsync(Guid expertId, CancellationToken cancellationToken);

    // Tek koşullu UPDATE: bakiye + delta >= 0 ise uygular ve yeni bakiyeyi döner;
    // değilse (ya da uzman yoksa) hiçbir şeyi değiştirmez ve null döner. Satır kilidi
    // eşzamanlı iki düşümü sıraya sokar; okuma-sonra-yazma yarışı yoktur.
    Task<int?> TryApplyDeltaAsync(Guid expertId, int delta, CancellationToken cancellationToken);
}
