using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PsikoAl.Common.Dtos.Auth;
using PsikoAl.Common.Dtos.Auth.Create;
using PsikoAl.Services.Abstractions;

namespace PsikoAl.Api.Controllers;

[ApiController]
[Route("auth")]
public sealed class AuthController(ISupabaseAuthService authService) : ControllerBase
{
    [HttpPost("login")]
    public Task<LoginResponseDto> Login(LoginRequestDto request, CancellationToken cancellationToken)
        => authService.LoginAsync(request, cancellationToken);

    [HttpPost("register")]
    public Task<LoginResponseDto> Register(RegisterRequestDto request, CancellationToken cancellationToken)
        => authService.RegisterAsync(request, cancellationToken);

    [HttpPost("refresh")]
    public Task<AuthTokensDto> Refresh(RefreshRequestDto request, CancellationToken cancellationToken)
        => authService.RefreshAsync(request, cancellationToken);

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        var accessToken = HttpContext.Request.Headers.Authorization.ToString().Replace("Bearer ", string.Empty);
        await authService.LogoutAsync(accessToken, cancellationToken);
        return NoContent();
    }
}
