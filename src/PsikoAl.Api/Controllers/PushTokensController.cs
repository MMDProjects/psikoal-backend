using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PsikoAl.Api.Extensions;
using PsikoAl.Common.Dtos.Notification;
using PsikoAl.Services.Abstractions;

namespace PsikoAl.Api.Controllers;

[ApiController]
[Route("push-tokens")]
[Authorize]
public sealed class PushTokensController(IPushTokenService pushTokenService) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Register(RegisterPushTokenDto request, CancellationToken cancellationToken)
    {
        await pushTokenService.RegisterAsync(this.CurrentUserId(), request, cancellationToken);
        return Ok(new { success = true });
    }

    [HttpPost("unregister")]
    public async Task<IActionResult> Unregister(UnregisterPushTokenDto request, CancellationToken cancellationToken)
    {
        await pushTokenService.UnregisterAsync(this.CurrentUserId(), request.Token, cancellationToken);
        return Ok(new { success = true });
    }
}
