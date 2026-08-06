using PsikoAl.Common.Dtos.Notification;

namespace PsikoAl.Services.Abstractions;

public interface IPushTokenService
{
    Task RegisterAsync(Guid userId, RegisterPushTokenDto request, CancellationToken cancellationToken);

    Task UnregisterAsync(Guid userId, string token, CancellationToken cancellationToken);
}
