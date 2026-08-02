using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PsikoAl.Common.Constants;
using PsikoAl.Common.Dtos.Admin;
using PsikoAl.Common.Exceptions;
using PsikoAl.Data.Entities;
using PsikoAl.Data.Repositories.Abstractions;
using PsikoAl.Services.Abstractions;

namespace PsikoAl.Services;

public sealed class AdminUserService(
    IUnitOfWork unitOfWork,
    ISupabaseAdminService supabaseAdmin,
    IAdminGuard adminGuard) : IAdminUserService
{
    public async Task<IReadOnlyList<AdminUserListItemDto>> ListUsersAsync(
        string? search,
        string? role,
        string? status,
        CancellationToken cancellationToken)
    {
        var query = unitOfWork.Profiles.Query();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{search.Trim()}%";
            query = query.Where(profile =>
                EF.Functions.ILike(profile.Email, pattern)
                || EF.Functions.ILike(profile.FirstName + " " + profile.LastName, pattern));
        }

        if (!string.IsNullOrWhiteSpace(role))
        {
            query = query.Where(profile => profile.Role == role);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(profile => profile.Status == status);
        }

        return await query
            .OrderByDescending(profile => profile.CreatedAt)
            .Select(profile => new AdminUserListItemDto(
                profile.Id,
                profile.Email,
                profile.FirstName + " " + profile.LastName,
                profile.Role,
                profile.IsVerified,
                profile.Status,
                profile.CreatedAt))
            .ToListAsync(cancellationToken);
    }

    public Task FreezeUserAsync(Guid actorAuthUserId, Guid targetUserId, string? reason, CancellationToken cancellationToken)
        => ChangeStatusAsync(actorAuthUserId, targetUserId, ProfileStatuses.Frozen, "admin.user_freeze", reason, cancellationToken);

    public Task UnfreezeUserAsync(Guid actorAuthUserId, Guid targetUserId, string? reason, CancellationToken cancellationToken)
        => ChangeStatusAsync(actorAuthUserId, targetUserId, ProfileStatuses.Active, "admin.user_unfreeze", reason, cancellationToken);

    private async Task ChangeStatusAsync(
        Guid actorAuthUserId,
        Guid targetUserId,
        string newStatus,
        string action,
        string? reason,
        CancellationToken cancellationToken)
    {
        var actor = await adminGuard.GetActiveAdminAsync(actorAuthUserId, cancellationToken)
            ?? throw new DomainException(ErrorKeys.AdminUserNotFound);

        var profile = await unitOfWork.Profiles.GetByIdAsync(targetUserId, cancellationToken)
            ?? throw new DomainException(ErrorKeys.ProfileNotFound);

        var oldStatus = profile.Status;
        profile.Status = newStatus;

        if (newStatus == ProfileStatuses.Frozen)
        {
            await supabaseAdmin.BanUserAsync(targetUserId, cancellationToken);
        }
        else
        {
            await supabaseAdmin.UnbanUserAsync(targetUserId, cancellationToken);
        }

        await unitOfWork.AuditLogs.AddAsync(
            new AuditLog
            {
                AdminUserId = actor.Id,
                ActorType = AuditActorTypes.Admin,
                Action = action,
                EntityType = "profile",
                EntityId = profile.Id.ToString(),
                OldValue = JsonSerializer.Serialize(new { status = oldStatus }),
                NewValue = JsonSerializer.Serialize(new { status = newStatus }),
                Reason = reason,
            },
            cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
