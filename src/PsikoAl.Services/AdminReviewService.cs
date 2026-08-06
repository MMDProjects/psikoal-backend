using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PsikoAl.Common.Constants;
using PsikoAl.Common.Dtos.Admin;
using PsikoAl.Common.Exceptions;
using PsikoAl.Data.Entities;
using PsikoAl.Data.Repositories.Abstractions;
using PsikoAl.Services.Abstractions;

namespace PsikoAl.Services;

public sealed class AdminReviewService(
    IUnitOfWork unitOfWork,
    IAdminGuard adminGuard) : IAdminReviewService
{
    public async Task<IReadOnlyList<AdminReviewListItemDto>> ListAsync(string? status, CancellationToken cancellationToken)
    {
        var query =
            from review in unitOfWork.Reviews.Query()
            join expertProfile in unitOfWork.Profiles.Query() on review.ExpertId equals expertProfile.Id
            join clientProfile in unitOfWork.Profiles.Query() on review.ClientId equals clientProfile.Id
            select new { review, expertProfile, clientProfile };

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(row => row.review.Status == status);
        }

        return await query
            .OrderByDescending(row => row.review.CreatedAt)
            .Select(row => new AdminReviewListItemDto(
                row.review.Id,
                row.review.ExpertId,
                row.expertProfile.FirstName + " " + row.expertProfile.LastName,
                row.clientProfile.FirstName + " " + row.clientProfile.LastName,
                row.review.Rating,
                row.review.Comment,
                row.review.Status,
                row.review.RejectionReason,
                row.review.CreatedAt))
            .ToListAsync(cancellationToken);
    }

    public async Task ApproveAsync(Guid actorAuthUserId, Guid reviewId, CancellationToken cancellationToken)
    {
        var actor = await GetRequiredActorAsync(actorAuthUserId, cancellationToken);
        var review = await GetRequiredReviewAsync(reviewId, cancellationToken);
        var oldStatus = review.Status;

        review.Status = ReviewStatuses.Approved;
        review.RejectionReason = null;
        review.ModeratedAt = DateTimeOffset.UtcNow;
        review.ModeratedBy = actor.Id;

        await AddAuditAsync(actor.Id, "admin.review_approve", review.Id, oldStatus, review.Status, null, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task RejectAsync(Guid actorAuthUserId, Guid reviewId, string reason, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new DomainException(ErrorKeys.ReviewRejectionReasonRequired, "reason");
        }

        var actor = await GetRequiredActorAsync(actorAuthUserId, cancellationToken);
        var review = await GetRequiredReviewAsync(reviewId, cancellationToken);
        var oldStatus = review.Status;

        review.Status = ReviewStatuses.Rejected;
        review.RejectionReason = reason;
        review.ModeratedAt = DateTimeOffset.UtcNow;
        review.ModeratedBy = actor.Id;

        await AddAuditAsync(actor.Id, "admin.review_reject", review.Id, oldStatus, review.Status, reason, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task<AdminUser> GetRequiredActorAsync(Guid actorAuthUserId, CancellationToken cancellationToken)
        => await adminGuard.GetActiveAdminAsync(actorAuthUserId, cancellationToken)
            ?? throw new DomainException(ErrorKeys.AdminUserNotFound);

    private async Task<Review> GetRequiredReviewAsync(Guid reviewId, CancellationToken cancellationToken)
    {
        var review = await unitOfWork.Reviews.GetByIdAsync(reviewId, cancellationToken)
            ?? throw new DomainException(ErrorKeys.ReviewNotFound);

        // GetByIdAsync (Find) tracked entity döner; repository'nin Query() metodu AsNoTracking
        // kullandığı için burada doğrudan Find'a düşüyoruz — SaveChanges'in state'i görmesi için gerekli.
        return review;
    }

    private async Task AddAuditAsync(
        Guid adminUserId,
        string action,
        Guid reviewId,
        string oldStatus,
        string newStatus,
        string? reason,
        CancellationToken cancellationToken)
        => await unitOfWork.AuditLogs.AddAsync(
            new AuditLog
            {
                AdminUserId = adminUserId,
                ActorType = AuditActorTypes.Admin,
                Action = action,
                EntityType = "review",
                EntityId = reviewId.ToString(),
                OldValue = JsonSerializer.Serialize(new { status = oldStatus }),
                NewValue = JsonSerializer.Serialize(new { status = newStatus }),
                Reason = reason,
            },
            cancellationToken);
}
