using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PsikoAl.Common.Constants;
using PsikoAl.Common.Dtos.Admin;
using PsikoAl.Common.Exceptions;
using PsikoAl.Common.Presentation;
using PsikoAl.Data.Entities;
using PsikoAl.Data.Repositories.Abstractions;
using PsikoAl.Services.Abstractions;

namespace PsikoAl.Services;

public sealed class AdminListingService(
    IUnitOfWork unitOfWork,
    IAdminGuard adminGuard) : IAdminListingService
{
    public async Task<IReadOnlyList<AdminListingListItemDto>> ListAsync(string? status, CancellationToken cancellationToken)
    {
        var query =
            from listing in unitOfWork.Listings.Query()
            join client in unitOfWork.Profiles.Query() on listing.ClientId equals client.Id
            select new { listing, client };

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(row => row.listing.Status == status);
        }

        var rows = await query.OrderByDescending(row => row.listing.CreatedAt).ToListAsync(cancellationToken);
        return [.. rows.Select(row => new AdminListingListItemDto(
            row.listing.Id,
            $"{row.client.FirstName} {row.client.LastName}",
            row.client.Email,
            row.listing.Title,
            row.listing.Specialization,
            BudgetLabelFormatter.Format(row.listing.BudgetMin, row.listing.BudgetMax),
            row.listing.Status,
            row.listing.CreatedAt))];
    }

    public async Task<AdminListingDetailDto> GetDetailAsync(Guid listingId, CancellationToken cancellationToken)
    {
        var listing = await GetRequiredListingAsync(listingId, cancellationToken);
        var client = listing.Client ?? throw new DomainException(ErrorKeys.ProfileNotFound);

        return new AdminListingDetailDto(
            listing.Id,
            $"{client.FirstName} {client.LastName}",
            client.Email,
            listing.Title,
            listing.Description,
            listing.Specialization,
            BudgetLabelFormatter.Format(listing.BudgetMin, listing.BudgetMax),
            listing.PreferredSessionType,
            listing.City,
            listing.Status,
            listing.RejectionReason,
            listing.ApprovedAt,
            listing.ExpiresAt,
            listing.CreatedAt);
    }

    public async Task ApproveAsync(Guid actorAuthUserId, Guid listingId, CancellationToken cancellationToken)
    {
        var actor = await GetRequiredActorAsync(actorAuthUserId, cancellationToken);
        var listing = await GetRequiredListingAsync(listingId, cancellationToken);

        if (listing.Status != ListingStatuses.PendingApproval)
        {
            throw new DomainException(ErrorKeys.ListingNotPendingApproval);
        }

        var oldStatus = listing.Status;
        var expireDays = await unitOfWork.SystemSettings.GetIntAsync(SystemSettingKeys.ListingExpireDays, cancellationToken);

        listing.Status = ListingStatuses.Open;
        listing.ApprovedAt = DateTimeOffset.UtcNow;
        listing.ApprovedBy = actor.Id;
        listing.ExpiresAt = DateTimeOffset.UtcNow.AddDays(expireDays);
        listing.RejectionReason = null;

        await AddStatusAuditAsync(actor.Id, "admin.listing_approve", listing.Id, oldStatus, listing.Status, null, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task RejectAsync(Guid actorAuthUserId, Guid listingId, string reason, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new DomainException(ErrorKeys.ListingRejectionReasonRequired, "reason");
        }

        var actor = await GetRequiredActorAsync(actorAuthUserId, cancellationToken);
        var listing = await GetRequiredListingAsync(listingId, cancellationToken);

        if (listing.Status != ListingStatuses.PendingApproval)
        {
            throw new DomainException(ErrorKeys.ListingNotPendingApproval);
        }

        var oldStatus = listing.Status;
        listing.Status = ListingStatuses.RejectedByAdmin;
        listing.RejectionReason = reason;

        await AddStatusAuditAsync(actor.Id, "admin.listing_reject", listing.Id, oldStatus, listing.Status, reason, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task CloseAsync(Guid actorAuthUserId, Guid listingId, CancellationToken cancellationToken)
    {
        var actor = await GetRequiredActorAsync(actorAuthUserId, cancellationToken);
        var listing = await GetRequiredListingAsync(listingId, cancellationToken);
        var oldStatus = listing.Status;

        listing.Status = ListingStatuses.Closed;

        await AddStatusAuditAsync(actor.Id, "admin.listing_close", listing.Id, oldStatus, listing.Status, "Admin override", cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task ExtendExpiryAsync(Guid actorAuthUserId, Guid listingId, int additionalDays, CancellationToken cancellationToken)
    {
        if (additionalDays <= 0)
        {
            throw new DomainException(ErrorKeys.ValidationFailed, "additionalDays");
        }

        var actor = await GetRequiredActorAsync(actorAuthUserId, cancellationToken);
        var listing = await GetRequiredListingAsync(listingId, cancellationToken);

        if (listing.Status != ListingStatuses.Open)
        {
            throw new DomainException(ErrorKeys.ListingNotOpen);
        }

        var oldExpiresAt = listing.ExpiresAt;
        listing.ExpiresAt = (listing.ExpiresAt ?? DateTimeOffset.UtcNow).AddDays(additionalDays);

        await AddAuditAsync(
            actor.Id,
            "admin.listing_extend",
            listing.Id,
            JsonSerializer.Serialize(new { expiresAt = oldExpiresAt }),
            JsonSerializer.Serialize(new { expiresAt = listing.ExpiresAt }),
            $"+{additionalDays} gün",
            cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task ReopenAsync(Guid actorAuthUserId, Guid listingId, string reason, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new DomainException(ErrorKeys.ListingReopenReasonRequired, "reason");
        }

        var actor = await GetRequiredActorAsync(actorAuthUserId, cancellationToken);
        var listing = await GetRequiredListingAsync(listingId, cancellationToken);

        // İstisnai yeniden açma: yalnızca EXPIRED'dan. MATCHED asla yeniden açılmaz (BRIEF kuralı).
        if (listing.Status != ListingStatuses.Expired)
        {
            throw new DomainException(ErrorKeys.ListingReopenNotExpired);
        }

        var oldStatus = listing.Status;
        var expireDays = await unitOfWork.SystemSettings.GetIntAsync(SystemSettingKeys.ListingExpireDays, cancellationToken);

        listing.Status = ListingStatuses.Open;
        listing.ExpiresAt = DateTimeOffset.UtcNow.AddDays(expireDays);

        await AddStatusAuditAsync(actor.Id, "admin.listing_reopen", listing.Id, oldStatus, listing.Status, reason, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task<AdminUser> GetRequiredActorAsync(Guid actorAuthUserId, CancellationToken cancellationToken)
        => await adminGuard.GetActiveAdminAsync(actorAuthUserId, cancellationToken)
            ?? throw new DomainException(ErrorKeys.AdminUserNotFound);

    private async Task<Listing> GetRequiredListingAsync(Guid listingId, CancellationToken cancellationToken)
        => await unitOfWork.Listings.GetWithClientAsync(listingId, cancellationToken)
            ?? throw new DomainException(ErrorKeys.ListingNotFound);

    private Task AddStatusAuditAsync(
        Guid adminUserId,
        string action,
        Guid listingId,
        string oldStatus,
        string newStatus,
        string? reason,
        CancellationToken cancellationToken)
        => AddAuditAsync(
            adminUserId,
            action,
            listingId,
            JsonSerializer.Serialize(new { status = oldStatus }),
            JsonSerializer.Serialize(new { status = newStatus }),
            reason,
            cancellationToken);

    private async Task AddAuditAsync(
        Guid adminUserId,
        string action,
        Guid listingId,
        string oldValue,
        string newValue,
        string? reason,
        CancellationToken cancellationToken)
        => await unitOfWork.AuditLogs.AddAsync(
            new AuditLog
            {
                AdminUserId = adminUserId,
                ActorType = AuditActorTypes.Admin,
                Action = action,
                EntityType = "listing",
                EntityId = listingId.ToString(),
                OldValue = oldValue,
                NewValue = newValue,
                Reason = reason,
            },
            cancellationToken);
}
