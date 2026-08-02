using PsikoAl.Common.Dtos.Auth;
using PsikoAl.Common.Dtos.Auth.Create;

namespace PsikoAl.Services.Abstractions;

public interface ISupabaseAuthService
{
    Task<LoginResponseDto> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken);

    Task<LoginResponseDto> RegisterAsync(RegisterRequestDto request, CancellationToken cancellationToken);

    Task<AuthTokensDto> RefreshAsync(RefreshRequestDto request, CancellationToken cancellationToken);

    Task LogoutAsync(string accessToken, CancellationToken cancellationToken);
}
