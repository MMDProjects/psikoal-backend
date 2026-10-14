using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PsikoAl.Api.Authorization;
using PsikoAl.Common.Dtos.Admin;
using PsikoAl.Services.Abstractions;

namespace PsikoAl.Api.Controllers;

[ApiController]
[Route("admin/audit-logs")]
[Authorize(Policy = AdminPolicies.SuperAdmin)]
public sealed class AdminAuditLogsController(IAdminAuditLogService auditLogService) : ControllerBase
{
    [HttpGet]
    public Task<AdminPagedResultDto<AdminAuditLogDto>> List(
        [FromQuery] AdminAuditLogQueryDto query,
        CancellationToken cancellationToken)
        => auditLogService.ListAsync(query, cancellationToken);
}
