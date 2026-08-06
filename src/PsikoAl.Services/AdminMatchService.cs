using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PsikoAl.Common.Constants;
using PsikoAl.Common.Dtos.Admin;
using PsikoAl.Common.Exceptions;
using PsikoAl.Data.Entities;
using PsikoAl.Data.Repositories.Abstractions;
using PsikoAl.Services.Abstractions;

namespace PsikoAl.Services;

public sealed class AdminMatchService(
    IUnitOfWork unitOfWork,
    IAdminGuard adminGuard) : IAdminMatchService
{
    public async Task<IReadOnlyList<AdminMatchListItemDto>> ListAsync(string? status, CancellationToken cancellationToken)
    {
        var query = unitOfWork.Matches.QueryWithDetails();
        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(match => match.Status == status);
        }

        var matches = await query.OrderByDescending(match => match.CreatedAt).ToListAsync(cancellationToken);
        return [.. matches.Select(match => new AdminMatchListItemDto(
            match.Id,
            match.Client is null ? "—" : $"{match.Client.FirstName} {match.Client.LastName}",
            match.Expert?.Profile is null ? "—" : $"{match.Expert.Profile.FirstName} {match.Expert.Profile.LastName}",
            match.Listing?.Title ?? "—",
            match.Status,
            match.ClientReleasedAt,
            match.ExpertReleasedAt,
            match.CreatedAt))];
    }

    public async Task<AdminMatchDetailDto> GetDetailAsync(Guid matchId, CancellationToken cancellationToken)
    {
        var match = await GetRequiredMatchAsync(matchId, cancellationToken);
        var client = match.Client ?? throw new DomainException(ErrorKeys.ProfileNotFound);

        return new AdminMatchDetailDto(
            match.Id,
            $"{client.FirstName} {client.LastName}",
            client.Email,
            match.Expert?.Profile is null ? "—" : $"{match.Expert.Profile.FirstName} {match.Expert.Profile.LastName}",
            match.Listing?.Title ?? "—",
            match.AcceptedOffer?.Price ?? 0,
            match.Status,
            match.ClientReleasedAt,
            match.ExpertReleasedAt,
            match.ReleasedByAdmin,
            match.ReleaseReason,
            match.CreatedAt);
    }

    public async Task ForceReleaseAsync(
        Guid actorAuthUserId,
        Guid matchId,
        string targetStatus,
        string reason,
        CancellationToken cancellationToken)
    {
        if (targetStatus is not (MatchStatuses.Released or MatchStatuses.Completed))
        {
            throw new DomainException(ErrorKeys.ValidationFailed, "targetStatus");
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new DomainException(ErrorKeys.MatchForceReleaseReasonRequired, "reason");
        }

        var actor = await adminGuard.GetActiveAdminAsync(actorAuthUserId, cancellationToken)
            ?? throw new DomainException(ErrorKeys.AdminUserNotFound);
        var match = await GetRequiredMatchAsync(matchId, cancellationToken);

        if (match.Status != MatchStatuses.Active)
        {
            throw new DomainException(ErrorKeys.MatchNotActive);
        }

        var oldStatus = match.Status;
        match.Status = targetStatus;
        match.ReleasedByAdmin = true;
        match.ReleaseReason = reason;
        match.ClientReleasedAt ??= DateTimeOffset.UtcNow;
        match.ExpertReleasedAt ??= DateTimeOffset.UtcNow;

        await unitOfWork.AuditLogs.AddAsync(
            new AuditLog
            {
                AdminUserId = actor.Id,
                ActorType = AuditActorTypes.Admin,
                Action = "admin.match_force_release",
                EntityType = "match",
                EntityId = match.Id.ToString(),
                OldValue = JsonSerializer.Serialize(new { status = oldStatus }),
                NewValue = JsonSerializer.Serialize(new { status = targetStatus }),
                Reason = reason,
            },
            cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task<Match> GetRequiredMatchAsync(Guid matchId, CancellationToken cancellationToken)
        => await unitOfWork.Matches.GetWithDetailsAsync(matchId, cancellationToken)
            ?? throw new DomainException(ErrorKeys.MatchNotFound);
}
