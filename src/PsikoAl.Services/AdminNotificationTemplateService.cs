using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PsikoAl.Common.Constants;
using PsikoAl.Common.Dtos.Admin;
using PsikoAl.Common.Exceptions;
using PsikoAl.Data.Entities;
using PsikoAl.Data.Repositories.Abstractions;
using PsikoAl.Services.Abstractions;

namespace PsikoAl.Services;

public sealed class AdminNotificationTemplateService(
    IUnitOfWork unitOfWork,
    IAdminGuard adminGuard) : IAdminNotificationTemplateService
{
    private static readonly JsonSerializerOptions AuditJsonOptions = new(JsonSerializerDefaults.Web);

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
        var actor = await adminGuard.GetActiveAdminAsync(actorAuthUserId, cancellationToken)
            ?? throw new DomainException(ErrorKeys.AdminUserNotFound);

        var template = await unitOfWork.NotificationTemplates.GetByTypeAsync(type, cancellationToken)
            ?? throw new DomainException(ErrorKeys.NotificationTemplateNotFound);

        var oldValue = JsonSerializer.Serialize(ToDto(template), AuditJsonOptions);

        template.Title = request.Title;
        template.Body = request.Body;
        template.HtmlBody = request.HtmlBody;
        template.PushEnabled = request.PushEnabled;
        template.EmailEnabled = request.EmailEnabled;
        template.InAppEnabled = request.InAppEnabled;
        template.IsActive = request.IsActive;

        var updated = ToDto(template);
        await unitOfWork.AuditLogs.AddAsync(
            new AuditLog
            {
                AdminUserId = actor.Id,
                ActorType = AuditActorTypes.Admin,
                Action = AuditActions.NotificationTemplateUpdate,
                EntityType = "notification_template",
                EntityId = template.Type,
                OldValue = oldValue,
                NewValue = JsonSerializer.Serialize(updated, AuditJsonOptions),
            },
            cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return updated;
    }

    private static AdminNotificationTemplateDto ToDto(NotificationTemplate template)
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
