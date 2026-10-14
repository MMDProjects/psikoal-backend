using Microsoft.EntityFrameworkCore;
using PsikoAl.Data.Entities;
using PsikoAl.Data.Repositories.Abstractions;

namespace PsikoAl.Data.Repositories;

public sealed class BlogRepository(AppDbContext dbContext)
    : Repository<Blog, Guid>(dbContext), IBlogRepository
{
    public Task<Blog?> GetPublicAsync(string slugOrId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var publicBlogs = DbContext.Blogs.AsNoTracking()
            .Include(blog => blog.Category)
            .Where(BlogVisibility.IsPublicAt(now));

        return Guid.TryParse(slugOrId, out var id)
            ? publicBlogs.FirstOrDefaultAsync(blog => blog.Id == id || blog.Slug == slugOrId, cancellationToken)
            : publicBlogs.FirstOrDefaultAsync(blog => blog.Slug == slugOrId, cancellationToken);
    }

    public Task<bool> SlugExistsAsync(string slug, Guid? exceptBlogId, CancellationToken cancellationToken)
        => DbContext.Blogs.AnyAsync(
            blog => blog.Slug == slug && (exceptBlogId == null || blog.Id != exceptBlogId),
            cancellationToken);
}
