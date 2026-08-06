using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PsikoAl.Api.Extensions;
using PsikoAl.Common.Dtos.Admin;
using PsikoAl.Services.Abstractions;

namespace PsikoAl.Api.Controllers;

[ApiController]
[Route("admin/matches")]
[Authorize(Policy = "Admin")]
public sealed class AdminMatchesController(IAdminMatchService adminMatchService) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyList<AdminMatchListItemDto>> List([FromQuery] string? status, CancellationToken cancellationToken)
        => adminMatchService.ListAsync(status, cancellationToken);

    [HttpGet("{id:guid}")]
    public Task<AdminMatchDetailDto> GetDetail(Guid id, CancellationToken cancellationToken)
        => adminMatchService.GetDetailAsync(id, cancellationToken);

    [HttpPost("{id:guid}/force-release")]
    public async Task<IActionResult> ForceRelease(Guid id, [FromBody] AdminForceReleaseMatchDto body, CancellationToken cancellationToken)
    {
        await adminMatchService.ForceReleaseAsync(this.CurrentUserId(), id, body.TargetStatus, body.Reason, cancellationToken);
        return Ok(new { success = true });
    }
}
