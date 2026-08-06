using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PsikoAl.Api.Extensions;
using PsikoAl.Common.Dtos.Admin;
using PsikoAl.Services.Abstractions;

namespace PsikoAl.Api.Controllers;

[ApiController]
[Route("admin/assessments")]
[Authorize(Policy = "Admin")]
public sealed class AdminAssessmentsController(IAdminAssessmentService adminAssessmentService) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyList<AdminAssessmentListItemDto>> List(CancellationToken cancellationToken)
        => adminAssessmentService.ListAsync(cancellationToken);

    [HttpGet("{id:guid}")]
    public Task<AdminAssessmentDetailDto> GetDetail(Guid id, CancellationToken cancellationToken)
        => adminAssessmentService.GetDetailAsync(id, cancellationToken);

    [HttpPatch("{id:guid}")]
    public Task<AdminAssessmentDetailDto> Update(Guid id, UpdateAdminAssessmentDto request, CancellationToken cancellationToken)
        => adminAssessmentService.UpdateAsync(this.CurrentUserId(), id, request, cancellationToken);

    [HttpPatch("score-rules/{id:guid}")]
    public Task<AdminScoreRuleDto> UpdateScoreRule(Guid id, UpdateAdminScoreRuleDto request, CancellationToken cancellationToken)
        => adminAssessmentService.UpdateScoreRuleAsync(this.CurrentUserId(), id, request, cancellationToken);
}
