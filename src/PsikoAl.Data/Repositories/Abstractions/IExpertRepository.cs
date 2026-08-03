using PsikoAl.Data.Entities;

namespace PsikoAl.Data.Repositories.Abstractions;

public interface IExpertRepository : IRepository<Expert, Guid>
{
    Task<Expert?> GetWithProfileAsync(Guid id, CancellationToken cancellationToken);
}
