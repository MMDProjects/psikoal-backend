using PsikoAl.Common.Dtos.Auth;
using PsikoAl.Common.Dtos.Auth.Create;

namespace PsikoAl.Services.Abstractions;

public interface IAuthService
{
    Task<LoginResponseDto> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken);

    Task<LoginResponseDto> RegisterAsync(RegisterRequestDto request, CancellationToken cancellationToken);

    Task ChangePasswordAsync(Guid userId, string email, ChangePasswordRequestDto request, CancellationToken cancellationToken);

    Task ForgotPasswordAsync(ForgotPasswordRequestDto request, CancellationToken cancellationToken);
}
