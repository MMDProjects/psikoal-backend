using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PsikoAl.Common.Dtos.Notification;
using PsikoAl.Common.Exceptions;
using PsikoAl.Common.Constants;
using PsikoAl.Common.Presentation;
using PsikoAl.Data.Entities;
using PsikoAl.Data.Repositories.Abstractions;
using PsikoAl.Services.Abstractions;
using PsikoAl.Services.Notifications;

namespace PsikoAl.Services;

public sealed class NotificationService(
    IUnitOfWork unitOfWork,
    IPushOutbox pushOutbox,
    IEmailService emailService,
    ILogger<NotificationService> logger) : INotificationService
{
    public async Task NotifyAsync(
        Guid userId,
        string type,
        IReadOnlyDictionary<string, string> variables,
        string? dataJson,
        CancellationToken cancellationToken)
    {
        var template = await unitOfWork.NotificationTemplates.GetByTypeAsync(type, cancellationToken);
        if (template is null || !template.IsActive)
        {
            return;
        }

        var title = Render(template.Title, variables);
        var body = Render(template.Body, variables);

        if (template.InAppEnabled)
        {
            await unitOfWork.Notifications.AddAsync(
                new Notification { UserId = userId, Type = type, Title = title, Body = body, Data = dataJson },
                cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        if (template.PushEnabled)
        {
            var (pushTitle, pushBody) = PushContent.TextFor(type, title, body);
            pushOutbox.TryEnqueue(new PushDispatch(userId, pushTitle, pushBody, PushContent.DataFor(type, dataJson)));
        }

        if (template.EmailEnabled)
        {
            await SendEmailSafeAsync(userId, title, template.HtmlBody, variables, body, cancellationToken);
        }
    }

    public async Task<NotificationListResult> ListMyAsync(Guid userId, CancellationToken cancellationToken)
    {
        var notifications = await unitOfWork.Notifications.Query()
            .Where(notification => notification.UserId == userId)
            .OrderByDescending(notification => notification.CreatedAt)
            .ToListAsync(cancellationToken);

        var unreadCount = await unitOfWork.Notifications.CountUnreadAsync(userId, cancellationToken);
        var dtos = notifications.Select(ToNotificationDto).ToList();
        return new NotificationListResult(dtos, dtos.Count, unreadCount);
    }

    public async Task MarkReadAsync(Guid userId, Guid notificationId, CancellationToken cancellationToken)
    {
        var notification = await unitOfWork.Notifications.GetByIdAsync(notificationId, cancellationToken)
            ?? throw new DomainException(ErrorKeys.NotificationNotFound);

        if (notification.UserId != userId)
        {
            throw new DomainException(ErrorKeys.NotificationNotFound);
        }

        notification.Read = true;
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public Task MarkAllReadAsync(Guid userId, CancellationToken cancellationToken)
        => unitOfWork.Notifications.MarkAllReadAsync(userId, cancellationToken);

    private async Task SendEmailSafeAsync(
        Guid userId,
        string title,
        string? htmlBodyTemplate,
        IReadOnlyDictionary<string, string> variables,
        string fallbackBody,
        CancellationToken cancellationToken)
    {
        try
        {
            var profile = await unitOfWork.Profiles.GetByIdAsync(userId, cancellationToken);
            if (profile is null)
            {
                return;
            }

            var htmlBody = htmlBodyTemplate is null
                ? $"<p>{fallbackBody}</p>"
                : Render(htmlBodyTemplate, variables);

            await emailService.SendAsync(profile.Email, $"{profile.FirstName} {profile.LastName}", title, htmlBody, cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "E-posta bildirimi gönderilemedi (userId: {UserId})", userId);
        }
    }

    private static string Render(string template, IReadOnlyDictionary<string, string> variables)
    {
        var result = template;
        foreach (var (key, value) in variables)
        {
            result = result.Replace("{" + key + "}", value);
        }

        return result;
    }

    private static NotificationDto ToNotificationDto(Notification notification)
        => new(
            notification.Id,
            notification.Type,
            notification.Title,
            notification.Body,
            notification.CreatedAt,
            RelativeTimeTr.From(notification.CreatedAt),
            notification.Read);
}
