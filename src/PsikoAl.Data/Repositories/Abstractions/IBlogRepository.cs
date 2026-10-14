using PsikoAl.Data.Entities;

namespace PsikoAl.Data.Repositories.Abstractions;

public interface IBlogRepository : IRepository<Blog, Guid>
{
    Task<Blog?> GetPublicAsync(string slugOrId, DateTimeOffset now, CancellationToken cancellationToken);

    Task<bool> SlugExistsAsync(string slug, Guid? exceptBlogId, CancellationToken cancellationToken);
}
