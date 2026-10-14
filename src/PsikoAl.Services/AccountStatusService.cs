using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using PsikoAl.Common.Constants;
using PsikoAl.Data.Repositories.Abstractions;
using PsikoAl.Services.Abstractions;

namespace PsikoAl.Services;

// Dondurulan/silinen hesabın elindeki access token süresi dolana kadar (Supabase'de
// 1 saat) geçerli kalıyordu; GoTrue ban'ı yalnız YENİ oturumu engeller. Her istekte
// profiles.status bakılır; DB yükünü sınırlamak için sonuç kısa süre önbellekte tutulur
// ve durum değiştiren her yol (dondur/çöz/sil) kaydı hemen düşürür.
public sealed class AccountStatusService(IUnitOfWork unitOfWork, IMemoryCache cache) : IAccountStatusService
{
    public static readonly TimeSpan CacheDuration = TimeSpan.FromSeconds(30);

    public async Task<bool> IsActiveAsync(Guid userId, CancellationToken cancellationToken)
    {
        if (cache.TryGetValue(CacheKey(userId), out bool isActive))
        {
            return isActive;
        }

        var status = await unitOfWork.Profiles.Query()
            .Where(profile => profile.Id == userId)
            .Select(profile => profile.Status)
            .FirstOrDefaultAsync(cancellationToken);

        // Profil satırı yoksa (ör. kayıt anındaki yarış ya da profilsiz yönetici hesabı)
        // engellenmez: bu kontrol yalnız AÇIKÇA pasifleştirilmiş hesabı durdurur.
        isActive = status is null or ProfileStatuses.Active;
        cache.Set(CacheKey(userId), isActive, CacheDuration);
        return isActive;
    }

    public void Invalidate(Guid userId) => cache.Remove(CacheKey(userId));

    private static string CacheKey(Guid userId) => $"account-status:{userId}";
}
