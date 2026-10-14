using Microsoft.EntityFrameworkCore;
using PsikoAl.Data.Entities;
using PsikoAl.Data.Repositories.Abstractions;

namespace PsikoAl.Data.Repositories;

public sealed class BlogLikeRepository(AppDbContext dbContext) : IBlogLikeRepository
{
    public Task<BlogLike?> FindAsync(Guid blogId, Guid userId, CancellationToken cancellationToken)
        => dbContext.BlogLikes.FindAsync([blogId, userId], cancellationToken).AsTask();

    public async Task AddAsync(BlogLike blogLike, CancellationToken cancellationToken)
        => await dbContext.BlogLikes.AddAsync(blogLike, cancellationToken);

    public void Delete(BlogLike blogLike) => dbContext.BlogLikes.Remove(blogLike);

    public Task<int> CountForBlogAsync(Guid blogId, CancellationToken cancellationToken)
        => dbContext.BlogLikes.CountAsync(like => like.BlogId == blogId, cancellationToken);
}
