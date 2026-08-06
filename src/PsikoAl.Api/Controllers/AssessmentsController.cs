using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PsikoAl.Api.Extensions;
using PsikoAl.Common.Dtos.Assessment;
using PsikoAl.Common.Dtos.Assessment.Create;
using PsikoAl.Services.Abstractions;

namespace PsikoAl.Api.Controllers;

// Assessment testi auth gerektirmez (bkz. CLAUDE.md geliştirici notları) — bu controller'da
// [Authorize] yalnızca "my" (kullanıcıya özel) uçlarda kullanılır.
[ApiController]
[Route("assessment")]
public sealed class AssessmentsController(IAssessmentService assessmentService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] string? category, CancellationToken cancellationToken)
    {
        var result = await assessmentService.ListAsync(category, cancellationToken);
        return Ok(new { data = result });
    }

    [HttpGet("active")]
    public async Task<IActionResult> GetActive(CancellationToken cancellationToken)
    {
        var result = await assessmentService.GetActiveAsync(cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost("submit")]
    public Task<AssessmentResultDto> Submit(SubmitAssessmentDto request, CancellationToken cancellationToken)
        => assessmentService.SubmitAsync(this.CurrentUserIdOrNull(), request, cancellationToken);

    [HttpGet("results/my")]
    [Authorize]
    public async Task<IActionResult> ListMyResults(CancellationToken cancellationToken)
    {
        var result = await assessmentService.ListMyResultsAsync(this.CurrentUserId(), cancellationToken);
        return Ok(new { data = result.Data, meta = new { page = 1, total = result.Total, perPage = result.Total } });
    }

    [HttpGet("results/{id:guid}")]
    public Task<AssessmentResultDto> GetResult(Guid id, CancellationToken cancellationToken)
        => assessmentService.GetResultAsync(id, cancellationToken);
}
