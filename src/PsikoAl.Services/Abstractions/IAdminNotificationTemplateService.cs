using PsikoAl.Common.Dtos.Admin;

namespace PsikoAl.Services.Abstractions;

public interface IAdminNotificationTemplateService
{
    Task<IReadOnlyList<AdminNotificationTemplateDto>> ListAsync(CancellationToken cancellationToken);

    Task<AdminNotificationTemplateDto> UpdateAsync(
        Guid actorAuthUserId,
        string type,
        UpdateAdminNotificationTemplateDto request,
        CancellationToken cancellationToken);
}
