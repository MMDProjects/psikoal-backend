namespace PsikoAl.Client.Services;

public interface IAdminAuthService
{
    Task<AdminApiResult> LoginAsync(string email, string password, CancellationToken cancellationToken);

    Task RestoreAsync(CancellationToken cancellationToken);

    Task<string?> GetValidAccessTokenAsync(CancellationToken cancellationToken);

    Task<bool> RefreshAsync(string? staleAccessToken, CancellationToken cancellationToken);

    Task LogoutAsync(CancellationToken cancellationToken);
}
