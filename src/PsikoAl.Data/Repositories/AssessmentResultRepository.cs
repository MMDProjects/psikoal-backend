using Microsoft.EntityFrameworkCore;
using PsikoAl.Data.Entities;
using PsikoAl.Data.Repositories.Abstractions;

namespace PsikoAl.Data.Repositories;

public sealed class AssessmentResultRepository(AppDbContext dbContext)
    : Repository<AssessmentResult, Guid>(dbContext), IAssessmentResultRepository
{
    public IQueryable<AssessmentResult> QueryWithAssessment()
        => DbContext.AssessmentResults.AsNoTracking().Include(result => result.Assessment);
}
