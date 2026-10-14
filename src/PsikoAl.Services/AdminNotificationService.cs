using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PsikoAl.Common.Constants;
using PsikoAl.Common.Dtos.Admin;
using PsikoAl.Common.Exceptions;
using PsikoAl.Data.Entities;
using PsikoAl.Data.Repositories.Abstractions;
using PsikoAl.Services.Abstractions;

namespace PsikoAl.Services;

public sealed class AdminNotificationService(
    IUnitOfWork unitOfWork,
    IAdminGuard adminGuard,
    INotificationService notificationService) : IAdminNotificationService
{
    private const string TargetTypeUser = "user";
    private const string TargetTypeSegment = "segment";

    public async Task<int> PreviewRecipientCountAsync(AdminSendNotificationDto request, CancellationToken cancellationToken)
        => (await ResolveRecipientsAsync(request, cancellationToken)).Count;

    public async Task<int> SendAsync(Guid actorAuthUserId, AdminSendNotificationDto request, CancellationToken cancellationToken)
    {
        var actor = await adminGuard.GetActiveAdminAsync(actorAuthUserId, cancellationToken)
            ?? throw new DomainException(ErrorKeys.AdminUserNotFound);

        var targetUserIds = await ResolveRecipientsAsync(request, cancellationToken);

        await unitOfWork.AuditLogs.AddAsync(
            new AuditLog
            {
                AdminUserId = actor.Id,
                ActorType = AuditActorTypes.Admin,
                Action = AuditActions.NotificationSend,
                EntityType = "notification",
                EntityId = request.TargetType == TargetTypeUser
                    ? request.UserId?.ToString() ?? string.Empty
                    : $"{TargetTypeSegment}:{request.Role}",
                NewValue = JsonSerializer.Serialize(new
                {
                    targetType = request.TargetType,
                    userId = request.UserId,
                    role = request.Role,
                    city = request.City,
                    specialization = request.Specialization,
                    title = request.Title,
                    body = request.Body,
                    recipientCount = targetUserIds.Count,
                }),
            },
            cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var variables = new Dictionary<string, string>
        {
            ["title"] = request.Title,
            ["body"] = request.Body,
        };
        foreach (var userId in targetUserIds)
        {
            await notificationService.NotifyAsync(userId, NotificationTypes.System, variables, dataJson: null, cancellationToken);
        }

        return targetUserIds.Count;
    }

    private Task<List<Guid>> ResolveRecipientsAsync(AdminSendNotificationDto request, CancellationToken cancellationToken)
        => request.TargetType switch
        {
            TargetTypeUser => ResolveSingleUserAsync(request.UserId, cancellationToken),
            TargetTypeSegment => ResolveSegmentAsync(request, cancellationToken),
            _ => throw new DomainException(ErrorKeys.ValidationFailed, "targetType"),
        };

    private async Task<List<Guid>> ResolveSingleUserAsync(Guid? userId, CancellationToken cancellationToken)
    {
        if (userId is null)
        {
            throw new DomainException(ErrorKeys.ValidationFailed, "userId");
        }

        return await ActiveProfiles()
            .Where(profile => profile.Id == userId)
            .Select(profile => profile.Id)
            .ToListAsync(cancellationToken);
    }

    private async Task<List<Guid>> ResolveSegmentAsync(AdminSendNotificationDto request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Role))
        {
            throw new DomainException(ErrorKeys.ValidationFailed, "role");
        }

        var query = ActiveProfiles().Where(profile => profile.Role == request.Role);

        if (!string.IsNullOrWhiteSpace(request.City))
        {
            query = query.Where(profile => profile.City == request.City);
        }

        if (!string.IsNullOrWhiteSpace(request.Specialization) && request.Role == ProfileRoles.Expert)
        {
            var expertIds = unitOfWork.Experts.Query()
                .Where(expert => expert.Specializations.Contains(request.Specialization))
                .Select(expert => expert.Id);
            query = query.Where(profile => expertIds.Contains(profile.Id));
        }

        return await query.Select(profile => profile.Id).ToListAsync(cancellationToken);
    }

    private IQueryable<Profile> ActiveProfiles()
        => unitOfWork.Profiles.Query().Where(profile => profile.Status == ProfileStatuses.Active);
}
