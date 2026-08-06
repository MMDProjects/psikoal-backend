using PsikoAl.Common.Dtos.Admin;

namespace PsikoAl.Services.Abstractions;

public interface IAdminListingService
{
    Task<IReadOnlyList<AdminListingListItemDto>> ListAsync(string? status, CancellationToken cancellationToken);

    Task<AdminListingDetailDto> GetDetailAsync(Guid listingId, CancellationToken cancellationToken);

    Task ApproveAsync(Guid actorAuthUserId, Guid listingId, CancellationToken cancellationToken);

    Task RejectAsync(Guid actorAuthUserId, Guid listingId, string reason, CancellationToken cancellationToken);

    Task CloseAsync(Guid actorAuthUserId, Guid listingId, CancellationToken cancellationToken);

    Task ExtendExpiryAsync(Guid actorAuthUserId, Guid listingId, int additionalDays, CancellationToken cancellationToken);

    Task ReopenAsync(Guid actorAuthUserId, Guid listingId, string reason, CancellationToken cancellationToken);
}
