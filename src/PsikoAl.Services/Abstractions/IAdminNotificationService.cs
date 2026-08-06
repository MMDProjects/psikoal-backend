using PsikoAl.Common.Dtos.Admin;

namespace PsikoAl.Services.Abstractions;

public interface IAdminNotificationService
{
    Task<int> SendAsync(Guid actorAuthUserId, AdminSendNotificationDto request, CancellationToken cancellationToken);
}
