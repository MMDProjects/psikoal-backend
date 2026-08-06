using PsikoAl.Data.Entities;

namespace PsikoAl.Data.Repositories.Abstractions;

public interface IAssessmentQuestionRepository : IRepository<AssessmentQuestion, Guid>
{
    Task<List<AssessmentQuestion>> ListForAssessmentAsync(Guid assessmentId, CancellationToken cancellationToken);
}
