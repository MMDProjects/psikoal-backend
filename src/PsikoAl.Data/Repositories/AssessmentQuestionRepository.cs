using Microsoft.EntityFrameworkCore;
using PsikoAl.Data.Entities;
using PsikoAl.Data.Repositories.Abstractions;

namespace PsikoAl.Data.Repositories;

public sealed class AssessmentQuestionRepository(AppDbContext dbContext)
    : Repository<AssessmentQuestion, Guid>(dbContext), IAssessmentQuestionRepository
{
    public Task<List<AssessmentQuestion>> ListForAssessmentAsync(Guid assessmentId, CancellationToken cancellationToken)
        => DbContext.AssessmentQuestions
            .Where(question => question.AssessmentId == assessmentId)
            .OrderBy(question => question.SortOrder)
            .ToListAsync(cancellationToken);
}
