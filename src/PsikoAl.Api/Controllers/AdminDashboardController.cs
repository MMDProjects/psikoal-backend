using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PsikoAl.Common.Dtos.Admin;
using PsikoAl.Services.Abstractions;

namespace PsikoAl.Api.Controllers;

[ApiController]
[Route("admin/dashboard")]
[Authorize(Policy = "Admin")]
public sealed class AdminDashboardController(IAdminDashboardService adminDashboardService) : ControllerBase
{
    [HttpGet("stats")]
    public Task<AdminDashboardStatsDto> Stats(CancellationToken cancellationToken)
        => adminDashboardService.GetStatsAsync(cancellationToken);
}
