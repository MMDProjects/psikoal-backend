using PsikoAl.Common.Dtos.Admin;

namespace PsikoAl.Services.Abstractions;

public interface IAdminAssessmentService
{
    Task<IReadOnlyList<AdminAssessmentListItemDto>> ListAsync(CancellationToken cancellationToken);

    Task<AdminAssessmentDetailDto> GetDetailAsync(Guid assessmentId, CancellationToken cancellationToken);

    Task<AdminAssessmentDetailDto> UpdateAsync(
        Guid actorAuthUserId,
        Guid assessmentId,
        UpdateAdminAssessmentDto request,
        CancellationToken cancellationToken);

    Task<AdminScoreRuleDto> UpdateScoreRuleAsync(
        Guid actorAuthUserId,
        Guid scoreRuleId,
        UpdateAdminScoreRuleDto request,
        CancellationToken cancellationToken);
}
