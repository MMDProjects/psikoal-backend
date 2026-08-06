using Microsoft.EntityFrameworkCore;
using PsikoAl.Data.Entities;
using PsikoAl.Data.Repositories.Abstractions;

namespace PsikoAl.Data.Repositories;

public sealed class CategoryRepository(AppDbContext dbContext)
    : Repository<Category, Guid>(dbContext), ICategoryRepository
{
    public Task<Category?> GetBySlugAsync(string slug, CancellationToken cancellationToken)
        => Query().FirstOrDefaultAsync(category => category.Slug == slug, cancellationToken);

    public async Task<bool> AllActiveNamesExistAsync(IEnumerable<string> names, CancellationToken cancellationToken)
    {
        var distinctNames = names.Distinct().ToList();
        var matchCount = await DbContext.Categories
            .Where(category => category.IsActive && distinctNames.Contains(category.Name))
            .CountAsync(cancellationToken);
        return matchCount == distinctNames.Count;
    }
}
