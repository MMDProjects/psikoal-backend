using PsikoAl.Common.Dtos.Notification;

namespace PsikoAl.Services.Abstractions;

public interface INotificationService
{
    // Şablon devre dışıysa veya bulunamazsa sessizce no-op geçer — bildirim üretimi
    // hiçbir zaman çağıran iş akışını (teklif/ilan/admin işlemi) kesintiye uğratmaz.
    Task NotifyAsync(
        Guid userId,
        string type,
        IReadOnlyDictionary<string, string> variables,
        string? dataJson,
        CancellationToken cancellationToken);

    Task<NotificationListResult> ListMyAsync(Guid userId, CancellationToken cancellationToken);

    Task MarkReadAsync(Guid userId, Guid notificationId, CancellationToken cancellationToken);

    Task MarkAllReadAsync(Guid userId, CancellationToken cancellationToken);
}
