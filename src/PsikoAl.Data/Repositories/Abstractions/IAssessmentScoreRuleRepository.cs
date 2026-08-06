using PsikoAl.Data.Entities;

namespace PsikoAl.Data.Repositories.Abstractions;

public interface IAssessmentScoreRuleRepository : IRepository<AssessmentScoreRule, Guid>
{
    Task<List<AssessmentScoreRule>> ListForAssessmentAsync(Guid assessmentId, CancellationToken cancellationToken);

    Task<AssessmentScoreRule?> FindMatchingRuleAsync(Guid assessmentId, int score, CancellationToken cancellationToken);
}
