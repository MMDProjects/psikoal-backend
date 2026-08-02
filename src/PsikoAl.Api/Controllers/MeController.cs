using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PsikoAl.Api.Extensions;
using PsikoAl.Common.Dtos.Auth;
using PsikoAl.Common.Dtos.Auth.Update;
using PsikoAl.Services.Abstractions;

namespace PsikoAl.Api.Controllers;

[ApiController]
[Route("auth")]
public sealed class MeController(IProfileService profileService) : ControllerBase
{
    [Authorize]
    [HttpGet("me")]
    public Task<AuthUserDto> GetMe(CancellationToken cancellationToken)
        => profileService.GetMeAsync(this.CurrentUserId(), cancellationToken);

    [Authorize]
    [HttpPatch("me")]
    public Task<AuthUserDto> UpdateMe(UpdateProfileDto request, CancellationToken cancellationToken)
        => profileService.UpdateMeAsync(this.CurrentUserId(), request, cancellationToken);

    [Authorize]
    [HttpDelete("me")]
    public async Task<IActionResult> DeleteMe(CancellationToken cancellationToken)
    {
        await profileService.DeleteMeAsync(this.CurrentUserId(), cancellationToken);
        return NoContent();
    }

    [Authorize]
    [HttpPost("freeze")]
    public async Task<IActionResult> Freeze(CancellationToken cancellationToken)
    {
        await profileService.FreezeMeAsync(this.CurrentUserId(), cancellationToken);
        return Ok(new { success = true });
    }

    [Authorize]
    [HttpGet("whoami")]
    public IActionResult WhoAmI()
        => Ok(new
        {
            userId = User.FindFirstValue(ClaimTypes.NameIdentifier),
            email = User.FindFirstValue(ClaimTypes.Email),
        });
}
