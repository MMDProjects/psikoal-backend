using PsikoAl.Common.Dtos.Auth;
using PsikoAl.Common.Dtos.Auth.Create;

namespace PsikoAl.Services.Abstractions;

public interface ISupabaseAuthService
{
    Task<SupabaseSessionDto> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken);

    Task<AuthTokensDto> RefreshAsync(RefreshRequestDto request, CancellationToken cancellationToken);

    Task LogoutAsync(string accessToken, CancellationToken cancellationToken);

    Task SendPasswordRecoveryAsync(string email, CancellationToken cancellationToken);
}
