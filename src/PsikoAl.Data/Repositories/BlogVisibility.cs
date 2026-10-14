using System.Linq.Expressions;
using PsikoAl.Common.Constants;
using PsikoAl.Data.Entities;

namespace PsikoAl.Data.Repositories;

public static class BlogVisibility
{
    public static Expression<Func<Blog, bool>> IsPublicAt(DateTimeOffset now)
        => blog => blog.CoverImagePath != null
            && blog.PublishedAt != null
            && (blog.Status == BlogStatuses.Published
                || (blog.Status == BlogStatuses.Scheduled && blog.PublishedAt <= now));

    public static bool IsLive(string status, DateTimeOffset? publishedAt, bool hasCover, DateTimeOffset now)
        => hasCover
            && publishedAt.HasValue
            && (status == BlogStatuses.Published
                || (status == BlogStatuses.Scheduled && publishedAt.Value <= now));

    public static IQueryable<Blog> WithTag(this IQueryable<Blog> blogs, string? tag)
        => string.IsNullOrWhiteSpace(tag)
            ? blogs
            : blogs.Where(blog => blog.Tags.Contains(tag)
                || (blog.Category != null && blog.Category.BlogTag == tag));
}
