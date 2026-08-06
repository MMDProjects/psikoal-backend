using PsikoAl.Common.Dtos.Admin;

namespace PsikoAl.Services.Abstractions;

public interface IAdminMatchService
{
    Task<IReadOnlyList<AdminMatchListItemDto>> ListAsync(string? status, CancellationToken cancellationToken);

    Task<AdminMatchDetailDto> GetDetailAsync(Guid matchId, CancellationToken cancellationToken);

    Task ForceReleaseAsync(
        Guid actorAuthUserId,
        Guid matchId,
        string targetStatus,
        string reason,
        CancellationToken cancellationToken);
}
