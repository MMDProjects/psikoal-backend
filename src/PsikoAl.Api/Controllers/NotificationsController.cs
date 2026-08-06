using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PsikoAl.Api.Extensions;
using PsikoAl.Services.Abstractions;

namespace PsikoAl.Api.Controllers;

[ApiController]
[Route("notifications")]
[Authorize]
public sealed class NotificationsController(INotificationService notificationService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> ListMy(CancellationToken cancellationToken)
    {
        var result = await notificationService.ListMyAsync(this.CurrentUserId(), cancellationToken);
        return Ok(new
        {
            data = result.Data,
            meta = new { page = 1, total = result.Total, perPage = result.Total, unreadCount = result.UnreadCount },
        });
    }

    [HttpPost("{id:guid}/read")]
    public async Task<IActionResult> MarkRead(Guid id, CancellationToken cancellationToken)
    {
        await notificationService.MarkReadAsync(this.CurrentUserId(), id, cancellationToken);
        return Ok(new { success = true });
    }

    [HttpPost("read-all")]
    public async Task<IActionResult> MarkAllRead(CancellationToken cancellationToken)
    {
        await notificationService.MarkAllReadAsync(this.CurrentUserId(), cancellationToken);
        return Ok(new { success = true });
    }
}
