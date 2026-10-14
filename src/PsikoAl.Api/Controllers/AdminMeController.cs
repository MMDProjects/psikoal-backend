using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PsikoAl.Api.Authorization;
using PsikoAl.Api.Extensions;
using PsikoAl.Common.Dtos.Admin;
using PsikoAl.Services.Abstractions;

namespace PsikoAl.Api.Controllers;

[ApiController]
[Route("admin/me")]
[Authorize(Policy = AdminPolicies.Any)]
public sealed class AdminMeController(IAdminGuard adminGuard) : ControllerBase
{
    [HttpGet]
    public Task<AdminMeDto> Get(CancellationToken cancellationToken)
        => adminGuard.GetCurrentAsync(this.CurrentUserId(), cancellationToken);
}
