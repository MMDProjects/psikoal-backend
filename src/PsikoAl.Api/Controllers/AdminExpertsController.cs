using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PsikoAl.Api.Extensions;
using PsikoAl.Common.Dtos.Admin;
using PsikoAl.Services.Abstractions;

namespace PsikoAl.Api.Controllers;

[ApiController]
[Route("admin/experts")]
[Authorize(Policy = "Admin")]
public sealed class AdminExpertsController(
    IAdminExpertService adminExpertService,
    ISupabaseStorageService storage) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyList<AdminExpertListItemDto>> List([FromQuery] string? status, CancellationToken cancellationToken)
        => adminExpertService.ListAsync(status, cancellationToken);

    [HttpGet("{id:guid}")]
    public Task<AdminExpertDetailDto> GetDetail(Guid id, CancellationToken cancellationToken)
        => adminExpertService.GetDetailAsync(id, cancellationToken);

    [HttpPost("{id:guid}/approve")]
    public async Task<IActionResult> Approve(Guid id, CancellationToken cancellationToken)
    {
        await adminExpertService.ApproveAsync(this.CurrentUserId(), id, cancellationToken);
        return Ok(new { success = true });
    }

    [HttpPost("{id:guid}/reject")]
    public async Task<IActionResult> Reject(Guid id, [FromBody] AdminActionReasonDto body, CancellationToken cancellationToken)
    {
        await adminExpertService.RejectAsync(this.CurrentUserId(), id, body.Reason ?? string.Empty, cancellationToken);
        return Ok(new { success = true });
    }

    [HttpPost("{id:guid}/verify")]
    public async Task<IActionResult> SetVerified(Guid id, [FromBody] AdminSetVerifiedDto body, CancellationToken cancellationToken)
    {
        await adminExpertService.SetVerifiedAsync(this.CurrentUserId(), id, body.IsVerified, cancellationToken);
        return Ok(new { success = true });
    }

    [HttpGet("{id:guid}/documents")]
    public async Task<IActionResult> GetDocumentUrls(Guid id, CancellationToken cancellationToken)
    {
        var detail = await adminExpertService.GetDetailAsync(id, cancellationToken);
        var validFor = TimeSpan.FromMinutes(15);

        var cvUrl = detail.Current.CvUrl is null
            ? null
            : await storage.CreateSignedUrlAsync("documents", detail.Current.CvUrl, validFor, cancellationToken);

        var certificateUrls = new List<string>();
        foreach (var certificatePath in detail.Current.Certificates)
        {
            certificateUrls.Add(await storage.CreateSignedUrlAsync("documents", certificatePath, validFor, cancellationToken));
        }

        return Ok(new { cvUrl, certificateUrls });
    }
}
