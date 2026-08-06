using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PsikoAl.Api.Extensions;
using PsikoAl.Common.Dtos.Admin;
using PsikoAl.Services.Abstractions;

namespace PsikoAl.Api.Controllers;

[ApiController]
[Route("admin/reviews")]
[Authorize(Policy = "Admin")]
public sealed class AdminReviewsController(IAdminReviewService adminReviewService) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyList<AdminReviewListItemDto>> List([FromQuery] string? status, CancellationToken cancellationToken)
        => adminReviewService.ListAsync(status, cancellationToken);

    [HttpPost("{id:guid}/approve")]
    public async Task<IActionResult> Approve(Guid id, CancellationToken cancellationToken)
    {
        await adminReviewService.ApproveAsync(this.CurrentUserId(), id, cancellationToken);
        return Ok(new { success = true });
    }

    [HttpPost("{id:guid}/reject")]
    public async Task<IActionResult> Reject(Guid id, [FromBody] AdminActionReasonDto body, CancellationToken cancellationToken)
    {
        await adminReviewService.RejectAsync(this.CurrentUserId(), id, body.Reason ?? string.Empty, cancellationToken);
        return Ok(new { success = true });
    }
}
