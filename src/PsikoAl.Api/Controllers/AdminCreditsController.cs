using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PsikoAl.Api.Authorization;
using PsikoAl.Api.Extensions;
using PsikoAl.Common.Dtos.Admin;
using PsikoAl.Services.Abstractions;

namespace PsikoAl.Api.Controllers;

[ApiController]
[Route("admin/credits")]
[Authorize(Policy = AdminPolicies.Finance)]
public sealed class AdminCreditsController(IAdminCreditService adminCreditService) : ControllerBase
{
    [HttpGet("packages")]
    public Task<IReadOnlyList<AdminCreditPackageDto>> ListPackages(CancellationToken cancellationToken)
        => adminCreditService.ListPackagesAsync(cancellationToken);

    [HttpPost("packages")]
    public Task<AdminCreditPackageDto> CreatePackage(CreateAdminCreditPackageDto request, CancellationToken cancellationToken)
        => adminCreditService.CreatePackageAsync(this.CurrentUserId(), request, cancellationToken);

    [HttpPut("packages/{id:guid}")]
    public Task<AdminCreditPackageDto> UpdatePackage(
        Guid id,
        UpdateAdminCreditPackageDto request,
        CancellationToken cancellationToken)
        => adminCreditService.UpdatePackageAsync(this.CurrentUserId(), id, request, cancellationToken);

    [HttpDelete("packages/{id:guid}")]
    public async Task<IActionResult> DeletePackage(Guid id, CancellationToken cancellationToken)
    {
        await adminCreditService.DeletePackageAsync(this.CurrentUserId(), id, cancellationToken);
        return Ok(new { success = true });
    }

    [HttpGet("payments")]
    public Task<AdminPagedResultDto<AdminCreditPaymentListItemDto>> ListPayments(
        [FromQuery] AdminCreditPaymentQueryDto query,
        CancellationToken cancellationToken)
        => adminCreditService.ListPaymentsAsync(query, cancellationToken);

    [HttpGet("experts/{expertId:guid}")]
    public Task<AdminExpertCreditsDto> GetExpertCredits(
        Guid expertId,
        [FromQuery] int? page,
        [FromQuery] int? perPage,
        CancellationToken cancellationToken)
        => adminCreditService.GetExpertCreditsAsync(expertId, page, perPage, cancellationToken);

    [HttpPost("experts/{expertId:guid}/adjust")]
    public Task<AdminExpertCreditsDto> Adjust(
        Guid expertId,
        AdminCreditAdjustDto request,
        CancellationToken cancellationToken)
        => adminCreditService.AdjustAsync(this.CurrentUserId(), expertId, request, cancellationToken);
}
