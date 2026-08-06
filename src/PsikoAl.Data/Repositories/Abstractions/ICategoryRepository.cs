using PsikoAl.Data.Entities;

namespace PsikoAl.Data.Repositories.Abstractions;

public interface ICategoryRepository : IRepository<Category, Guid>
{
    Task<Category?> GetBySlugAsync(string slug, CancellationToken cancellationToken);

    Task<bool> AllActiveNamesExistAsync(IEnumerable<string> names, CancellationToken cancellationToken);
}
