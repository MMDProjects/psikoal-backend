using PsikoAl.Common.Dtos.Admin;

namespace PsikoAl.Services.Abstractions;

public interface IAdminExpertService
{
    Task<IReadOnlyList<AdminExpertListItemDto>> ListAsync(string? status, CancellationToken cancellationToken);

    Task<AdminExpertDetailDto> GetDetailAsync(Guid expertId, CancellationToken cancellationToken);

    Task ApproveAsync(Guid actorAuthUserId, Guid expertId, CancellationToken cancellationToken);

    Task RejectAsync(Guid actorAuthUserId, Guid expertId, string reason, CancellationToken cancellationToken);

    Task SetVerifiedAsync(Guid actorAuthUserId, Guid expertId, bool isVerified, CancellationToken cancellationToken);
}
