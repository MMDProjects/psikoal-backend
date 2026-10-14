using Microsoft.EntityFrameworkCore;
using PsikoAl.Data.Entities;
using PsikoAl.Data.Repositories.Abstractions;

namespace PsikoAl.Data.Repositories;

public sealed class CreditPackageRepository(AppDbContext dbContext)
    : Repository<CreditPackage, Guid>(dbContext), ICreditPackageRepository
{
    public async Task<IReadOnlyList<CreditPackage>> ListActiveAsync(CancellationToken cancellationToken)
        => await DbContext.CreditPackages
            .AsNoTracking()
            .Where(package => package.IsActive)
            .OrderBy(package => package.SortOrder)
            .ThenBy(package => package.Credits)
            .ToListAsync(cancellationToken);
}
