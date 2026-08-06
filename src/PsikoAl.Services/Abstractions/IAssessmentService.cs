using PsikoAl.Common.Dtos.Assessment;
using PsikoAl.Common.Dtos.Assessment.Create;

namespace PsikoAl.Services.Abstractions;

public interface IAssessmentService
{
    Task<IReadOnlyList<AssessmentListItemDto>> ListAsync(string? category, CancellationToken cancellationToken);

    Task<AssessmentDto?> GetActiveAsync(CancellationToken cancellationToken);

    Task<AssessmentResultDto> SubmitAsync(Guid? userId, SubmitAssessmentDto request, CancellationToken cancellationToken);

    Task<AssessmentResultDto> GetResultAsync(Guid resultId, CancellationToken cancellationToken);

    Task<MyAssessmentResultListResult> ListMyResultsAsync(Guid userId, CancellationToken cancellationToken);
}
