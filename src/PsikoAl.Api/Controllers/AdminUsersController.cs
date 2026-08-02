using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PsikoAl.Api.Extensions;
using PsikoAl.Common.Dtos.Admin;
using PsikoAl.Services.Abstractions;

namespace PsikoAl.Api.Controllers;

[ApiController]
[Route("admin/users")]
[Authorize(Policy = "Admin")]
public sealed class AdminUsersController(IAdminUserService adminUserService) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyList<AdminUserListItemDto>> List(
        [FromQuery] string? search,
        [FromQuery] string? role,
        [FromQuery] string? status,
        CancellationToken cancellationToken)
        => adminUserService.ListUsersAsync(search, role, status, cancellationToken);

    [HttpPost("{id:guid}/freeze")]
    public async Task<IActionResult> Freeze(Guid id, [FromBody] AdminActionReasonDto? body, CancellationToken cancellationToken)
    {
        await adminUserService.FreezeUserAsync(this.CurrentUserId(), id, body?.Reason, cancellationToken);
        return Ok(new { success = true });
    }

    [HttpPost("{id:guid}/unfreeze")]
    public async Task<IActionResult> Unfreeze(Guid id, [FromBody] AdminActionReasonDto? body, CancellationToken cancellationToken)
    {
        await adminUserService.UnfreezeUserAsync(this.CurrentUserId(), id, body?.Reason, cancellationToken);
        return Ok(new { success = true });
    }
}
