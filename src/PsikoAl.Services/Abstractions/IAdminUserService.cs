using PsikoAl.Common.Dtos.Admin;

namespace PsikoAl.Services.Abstractions;

public interface IAdminUserService
{
    Task<IReadOnlyList<AdminUserListItemDto>> ListUsersAsync(
        string? search,
        string? role,
        string? status,
        CancellationToken cancellationToken);

    Task FreezeUserAsync(Guid actorAuthUserId, Guid targetUserId, string? reason, CancellationToken cancellationToken);

    Task UnfreezeUserAsync(Guid actorAuthUserId, Guid targetUserId, string? reason, CancellationToken cancellationToken);
}
