using PsikoAl.Common.Dtos.Auth;
using PsikoAl.Common.Dtos.Auth.Create;
using PsikoAl.Services.Abstractions;

namespace PsikoAl.Services.Tests.Support;

public sealed class FakeSupabaseAuthService(Guid sessionUserId) : ISupabaseAuthService
{
    public Task<SupabaseSessionDto> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken)
        => Task.FromResult(new SupabaseSessionDto(sessionUserId, new AuthTokensDto("access", "refresh", 3600)));

    public Task<AuthTokensDto> RefreshAsync(RefreshRequestDto request, CancellationToken cancellationToken)
        => Task.FromResult(new AuthTokensDto("access", "refresh", 3600));

    public Task LogoutAsync(string accessToken, CancellationToken cancellationToken) => Task.CompletedTask;

    public Task SendPasswordRecoveryAsync(string email, CancellationToken cancellationToken) => Task.CompletedTask;

    public Task ResetPasswordAsync(string recoveryAccessToken, string newPassword, CancellationToken cancellationToken)
        => Task.CompletedTask;
}
