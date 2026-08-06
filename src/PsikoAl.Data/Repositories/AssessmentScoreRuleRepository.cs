using Microsoft.EntityFrameworkCore;
using PsikoAl.Data.Entities;
using PsikoAl.Data.Repositories.Abstractions;

namespace PsikoAl.Data.Repositories;

public sealed class AssessmentScoreRuleRepository(AppDbContext dbContext)
    : Repository<AssessmentScoreRule, Guid>(dbContext), IAssessmentScoreRuleRepository
{
    public Task<List<AssessmentScoreRule>> ListForAssessmentAsync(Guid assessmentId, CancellationToken cancellationToken)
        => DbContext.AssessmentScoreRules
            .Where(rule => rule.AssessmentId == assessmentId)
            .OrderBy(rule => rule.SortOrder)
            .ToListAsync(cancellationToken);

    public Task<AssessmentScoreRule?> FindMatchingRuleAsync(Guid assessmentId, int score, CancellationToken cancellationToken)
        => DbContext.AssessmentScoreRules
            .Where(rule => rule.AssessmentId == assessmentId && score >= rule.MinScore && score <= rule.MaxScore)
            .OrderBy(rule => rule.SortOrder)
            .FirstOrDefaultAsync(cancellationToken);
}
