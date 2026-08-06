using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PsikoAl.Api.Extensions;
using PsikoAl.Common.Dtos.Admin;
using PsikoAl.Services.Abstractions;

namespace PsikoAl.Api.Controllers;

[ApiController]
[Route("admin/notification-templates")]
[Authorize(Policy = "Admin")]
public sealed class AdminNotificationTemplatesController(IAdminNotificationTemplateService templateService) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyList<AdminNotificationTemplateDto>> List(CancellationToken cancellationToken)
        => templateService.ListAsync(cancellationToken);

    [HttpPatch("{type}")]
    public Task<AdminNotificationTemplateDto> Update(
        string type,
        UpdateAdminNotificationTemplateDto request,
        CancellationToken cancellationToken)
        => templateService.UpdateAsync(this.CurrentUserId(), type, request, cancellationToken);
}
