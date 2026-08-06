using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PsikoAl.Api.Extensions;
using PsikoAl.Common.Dtos.Admin;
using PsikoAl.Services.Abstractions;

namespace PsikoAl.Api.Controllers;

[ApiController]
[Route("admin/notifications")]
[Authorize(Policy = "Admin")]
public sealed class AdminNotificationsController(IAdminNotificationService adminNotificationService) : ControllerBase
{
    [HttpPost("send")]
    public async Task<IActionResult> Send(AdminSendNotificationDto request, CancellationToken cancellationToken)
    {
        var recipientCount = await adminNotificationService.SendAsync(this.CurrentUserId(), request, cancellationToken);
        return Ok(new { success = true, recipientCount });
    }
}
