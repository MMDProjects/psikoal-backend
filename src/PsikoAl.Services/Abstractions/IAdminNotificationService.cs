using PsikoAl.Common.Dtos.Admin;

namespace PsikoAl.Services.Abstractions;

public interface IAdminNotificationService
{
    Task<int> PreviewRecipientCountAsync(AdminSendNotificationDto request, CancellationToken cancellationToken);

    Task<int> SendAsync(Guid actorAuthUserId, AdminSendNotificationDto request, CancellationToken cancellationToken);
}
