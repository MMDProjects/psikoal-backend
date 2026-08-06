using PsikoAl.Data.Entities;
using PsikoAl.Data.Repositories.Abstractions;

namespace PsikoAl.Data.Repositories;

public sealed class AssessmentRepository(AppDbContext dbContext)
    : Repository<Assessment, Guid>(dbContext), IAssessmentRepository
{
}
