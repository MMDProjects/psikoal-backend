using PsikoAl.Data.Repositories.Abstractions;
using PsikoAl.Services.Abstractions;

namespace PsikoAl.Services.Notifications;

// Kuyruktan alınan tek bir push'u gönderir; arka plan işçisi her öğe için ayrı DI
// scope'unda bunu çağırır (DbContext istekler arasında paylaşılamaz).
public sealed class PushDispatcher(IUnitOfWork unitOfWork, IPushNotificationService pushNotificationService)
{
    public async Task DispatchAsync(PushDispatch dispatch, CancellationToken cancellationToken)
    {
        var tokens = await unitOfWork.PushTokens.GetTokensForUserAsync(dispatch.UserId, cancellationToken);
        if (tokens.Count == 0)
        {
            return;
        }

        var invalidTokens = await pushNotificationService.SendAsync(
            tokens,
            dispatch.Title,
            dispatch.Body,
            dispatch.DataJson,
            cancellationToken);

        foreach (var invalidToken in invalidTokens)
        {
            await unitOfWork.PushTokens.DeleteByTokenAsync(invalidToken, cancellationToken);
        }
    }
}
