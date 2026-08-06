using Microsoft.EntityFrameworkCore;
using PsikoAl.Common.Constants;
using PsikoAl.Common.Dtos.Admin;
using PsikoAl.Common.Exceptions;
using PsikoAl.Data.Repositories.Abstractions;
using PsikoAl.Services.Abstractions;

namespace PsikoAl.Services;

public sealed class AdminNotificationTemplateService(
    IUnitOfWork unitOfWork,
    IAdminGuard adminGuard) : IAdminNotificationTemplateService
{
    public async Task<IReadOnlyList<AdminNotificationTemplateDto>> ListAsync(CancellationToken cancellationToken)
    {
        var templates = await unitOfWork.NotificationTemplates.Query()
            .OrderBy(template => template.Type)
            .ToListAsync(cancellationToken);

        return [.. templates.Select(ToDto)];
    }

    public async Task<AdminNotificationTemplateDto> UpdateAsync(
        Guid actorAuthUserId,
        string type,
        UpdateAdminNotificationTemplateDto request,
        CancellationToken cancellationToken)
    {
        _ = await adminGuard.GetActiveAdminAsync(actorAuthUserId, cancellationToken)
            ?? throw new DomainException(ErrorKeys.AdminUserNotFound);

        var template = await unitOfWork.NotificationTemplates.GetByTypeAsync(type, cancellationToken)
            ?? throw new DomainException(ErrorKeys.NotificationTemplateNotFound);

        template.Title = request.Title;
        template.Body = request.Body;
        template.HtmlBody = request.HtmlBody;
        template.PushEnabled = request.PushEnabled;
        template.EmailEnabled = request.EmailEnabled;
        template.InAppEnabled = request.InAppEnabled;
        template.IsActive = request.IsActive;

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ToDto(template);
    }

    private static AdminNotificationTemplateDto ToDto(Data.Entities.NotificationTemplate template)
        => new(
            template.Id,
            template.Type,
            template.Title,
            template.Body,
            template.HtmlBody,
            template.PushEnabled,
            template.EmailEnabled,
            template.InAppEnabled,
            template.IsActive);
}
