using PsikoAl.Data.Entities;

namespace PsikoAl.Data.Repositories.Abstractions;

public interface IAssessmentResultRepository : IRepository<AssessmentResult, Guid>
{
    IQueryable<AssessmentResult> QueryWithAssessment();
}
