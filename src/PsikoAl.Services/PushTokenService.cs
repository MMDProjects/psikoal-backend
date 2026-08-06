using PsikoAl.Common.Dtos.Notification;
using PsikoAl.Data.Entities;
using PsikoAl.Data.Repositories.Abstractions;
using PsikoAl.Services.Abstractions;

namespace PsikoAl.Services;

public sealed class PushTokenService(IUnitOfWork unitOfWork) : IPushTokenService
{
    public async Task RegisterAsync(Guid userId, RegisterPushTokenDto request, CancellationToken cancellationToken)
    {
        var existing = await unitOfWork.PushTokens.GetByTokenAsync(request.Token, cancellationToken);
        if (existing is not null)
        {
            // Cihaz farklı bir hesaba geçmiş olabilir (çıkış yapıp başka kullanıcıyla giriş) —
            // token'ı yeni sahibine devrederiz, aynı cihaza iki kayıt açmayız.
            existing.UserId = userId;
            existing.Platform = request.Platform;
            existing.DeviceId = request.DeviceId;
            existing.LastSeenAt = DateTimeOffset.UtcNow;
        }
        else
        {
            await unitOfWork.PushTokens.AddAsync(
                new PushToken
                {
                    UserId = userId,
                    Token = request.Token,
                    Platform = request.Platform,
                    DeviceId = request.DeviceId,
                },
                cancellationToken);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task UnregisterAsync(Guid userId, string token, CancellationToken cancellationToken)
    {
        var existing = await unitOfWork.PushTokens.GetByTokenAsync(token, cancellationToken);
        if (existing is not null && existing.UserId == userId)
        {
            await unitOfWork.PushTokens.DeleteByTokenAsync(token, cancellationToken);
        }
    }
}
