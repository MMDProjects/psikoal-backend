using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using PsikoAl.Api.Extensions;
using PsikoAl.Common.Dtos.Auth;
using PsikoAl.Common.Dtos.Auth.Create;
using PsikoAl.Services.Abstractions;

namespace PsikoAl.Api.Controllers;

[ApiController]
[Route("auth")]
public sealed class AuthController(
    IAuthService authService,
    ISupabaseAuthService supabaseAuth) : ControllerBase
{
    [HttpPost("login")]
    [EnableRateLimiting("auth")]
    public Task<LoginResponseDto> Login(LoginRequestDto request, CancellationToken cancellationToken)
        => authService.LoginAsync(request, cancellationToken);

    [HttpPost("register")]
    [EnableRateLimiting("auth")]
    public Task<LoginResponseDto> Register(RegisterRequestDto request, CancellationToken cancellationToken)
        => authService.RegisterAsync(request, cancellationToken);

    [HttpPost("refresh")]
    public Task<AuthTokensDto> Refresh(RefreshRequestDto request, CancellationToken cancellationToken)
        => supabaseAuth.RefreshAsync(request, cancellationToken);

    [Authorize]
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword(ChangePasswordRequestDto request, CancellationToken cancellationToken)
    {
        await authService.ChangePasswordAsync(this.CurrentUserId(), this.CurrentUserEmail(), request, cancellationToken);
        return Ok(new { success = true });
    }

    [HttpPost("forgot-password")]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordRequestDto request, CancellationToken cancellationToken)
    {
        await authService.ForgotPasswordAsync(request, cancellationToken);
        return Ok(new { success = true });
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        var accessToken = HttpContext.Request.Headers.Authorization.ToString().Replace("Bearer ", string.Empty);
        await supabaseAuth.LogoutAsync(accessToken, cancellationToken);
        return NoContent();
    }
}
