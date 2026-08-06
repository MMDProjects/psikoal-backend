using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PsikoAl.Api.Extensions;
using PsikoAl.Common.Dtos.Admin;
using PsikoAl.Services.Abstractions;

namespace PsikoAl.Api.Controllers;

[ApiController]
[Route("admin/system-settings")]
[Authorize(Policy = "Admin")]
public sealed class AdminSystemSettingsController(ISystemSettingsService systemSettingsService) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyList<SystemSettingDto>> List(CancellationToken cancellationToken)
        => systemSettingsService.ListAsync(cancellationToken);

    [HttpPatch("{key}")]
    public Task<SystemSettingDto> Update(string key, UpdateSystemSettingDto request, CancellationToken cancellationToken)
        => systemSettingsService.UpdateAsync(this.CurrentUserId(), key, request.Value, cancellationToken);
}
