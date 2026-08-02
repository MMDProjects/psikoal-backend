using PsikoAl.Common.Dtos.Admin;

namespace PsikoAl.Client.Services;

public interface IAdminClientService
{
    Task<bool> LoginAsync(string email, string password, CancellationToken cancellationToken);

    Task<IReadOnlyList<AdminUserListItemDto>?> ListUsersAsync(
        string? search,
        string? role,
        string? status,
        CancellationToken cancellationToken);

    Task<bool> FreezeUserAsync(Guid userId, string? reason, CancellationToken cancellationToken);

    Task<bool> UnfreezeUserAsync(Guid userId, string? reason, CancellationToken cancellationToken);
}
