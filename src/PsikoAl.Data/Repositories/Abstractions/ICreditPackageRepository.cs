using PsikoAl.Data.Entities;

namespace PsikoAl.Data.Repositories.Abstractions;

public interface ICreditPackageRepository : IRepository<CreditPackage, Guid>
{
    Task<IReadOnlyList<CreditPackage>> ListActiveAsync(CancellationToken cancellationToken);
}
