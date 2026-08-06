using PsikoAl.Common.Dtos.Admin;

namespace PsikoAl.Services.Abstractions;

public interface IAdminReviewService
{
    Task<IReadOnlyList<AdminReviewListItemDto>> ListAsync(string? status, CancellationToken cancellationToken);

    Task ApproveAsync(Guid actorAuthUserId, Guid reviewId, CancellationToken cancellationToken);

    Task RejectAsync(Guid actorAuthUserId, Guid reviewId, string reason, CancellationToken cancellationToken);
}
