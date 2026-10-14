using PsikoAl.Data.Entities;

namespace PsikoAl.Data.Repositories.Abstractions;

public interface IBlogLikeRepository
{
    Task<BlogLike?> FindAsync(Guid blogId, Guid userId, CancellationToken cancellationToken);

    Task AddAsync(BlogLike blogLike, CancellationToken cancellationToken);

    void Delete(BlogLike blogLike);

    Task<int> CountForBlogAsync(Guid blogId, CancellationToken cancellationToken);
}
