using Microsoft.EntityFrameworkCore;
using PsikoAl.Common.Constants;
using PsikoAl.Common.Dtos.Admin;
using PsikoAl.Common.Exceptions;
using PsikoAl.Data.Repositories.Abstractions;
using PsikoAl.Services.Abstractions;

namespace PsikoAl.Services;

public sealed class AdminNotificationService(
    IUnitOfWork unitOfWork,
    IAdminGuard adminGuard,
    INotificationService notificationService) : IAdminNotificationService
{
    public async Task<int> SendAsync(Guid actorAuthUserId, AdminSendNotificationDto request, CancellationToken cancellationToken)
    {
        _ = await adminGuard.GetActiveAdminAsync(actorAuthUserId, cancellationToken)
            ?? throw new DomainException(ErrorKeys.AdminUserNotFound);

        var targetUserIds = request.TargetType switch
        {
            "user" => await ResolveSingleUserAsync(request.UserId, cancellationToken),
            "segment" => await ResolveSegmentAsync(request, cancellationToken),
            _ => throw new DomainException(ErrorKeys.ValidationFailed, "targetType"),
        };

        var variables = new Dictionary<string, string> { ["body"] = request.Body };
        foreach (var userId in targetUserIds)
        {
            await notificationService.NotifyAsync(userId, NotificationTypes.System, variables, dataJson: null, cancellationToken);
        }

        return targetUserIds.Count;
    }

    private async Task<List<Guid>> ResolveSingleUserAsync(Guid? userId, CancellationToken cancellationToken)
    {
        if (userId is null)
        {
            throw new DomainException(ErrorKeys.ValidationFailed, "userId");
        }

        return await unitOfWork.Profiles.Query()
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

        var query = unitOfWork.Profiles.Query().Where(profile => profile.Role == request.Role);

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
}
