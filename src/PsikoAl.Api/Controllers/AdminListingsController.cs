using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PsikoAl.Api.Extensions;
using PsikoAl.Common.Dtos.Admin;
using PsikoAl.Services.Abstractions;

namespace PsikoAl.Api.Controllers;

[ApiController]
[Route("admin/listings")]
[Authorize(Policy = "Admin")]
public sealed class AdminListingsController(IAdminListingService adminListingService) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyList<AdminListingListItemDto>> List([FromQuery] string? status, CancellationToken cancellationToken)
        => adminListingService.ListAsync(status, cancellationToken);

    [HttpGet("{id:guid}")]
    public Task<AdminListingDetailDto> GetDetail(Guid id, CancellationToken cancellationToken)
        => adminListingService.GetDetailAsync(id, cancellationToken);

    [HttpPost("{id:guid}/approve")]
    public async Task<IActionResult> Approve(Guid id, CancellationToken cancellationToken)
    {
        await adminListingService.ApproveAsync(this.CurrentUserId(), id, cancellationToken);
        return Ok(new { success = true });
    }

    [HttpPost("{id:guid}/reject")]
    public async Task<IActionResult> Reject(Guid id, [FromBody] AdminActionReasonDto body, CancellationToken cancellationToken)
    {
        await adminListingService.RejectAsync(this.CurrentUserId(), id, body.Reason ?? string.Empty, cancellationToken);
        return Ok(new { success = true });
    }

    [HttpPost("{id:guid}/close")]
    public async Task<IActionResult> Close(Guid id, CancellationToken cancellationToken)
    {
        await adminListingService.CloseAsync(this.CurrentUserId(), id, cancellationToken);
        return Ok(new { success = true });
    }

    [HttpPost("{id:guid}/extend")]
    public async Task<IActionResult> Extend(Guid id, AdminExtendListingDto body, CancellationToken cancellationToken)
    {
        await adminListingService.ExtendExpiryAsync(this.CurrentUserId(), id, body.AdditionalDays, cancellationToken);
        return Ok(new { success = true });
    }

    [HttpPost("{id:guid}/reopen")]
    public async Task<IActionResult> Reopen(Guid id, [FromBody] AdminActionReasonDto body, CancellationToken cancellationToken)
    {
        await adminListingService.ReopenAsync(this.CurrentUserId(), id, body.Reason ?? string.Empty, cancellationToken);
        return Ok(new { success = true });
    }
}
