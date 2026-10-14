using PsikoAl.Common.Constants;
using PsikoAl.Common.Exceptions;
using PsikoAl.Data.Entities;
using PsikoAl.Data.Repositories;

namespace PsikoAl.Services;

public static class BlogPublishRules
{
    public static void EnsurePublishable(Blog blog)
    {
        if (string.IsNullOrWhiteSpace(blog.CoverImagePath))
        {
            throw new DomainException(ErrorKeys.BlogNotPublishable, "coverImage");
        }

        if (string.IsNullOrWhiteSpace(blog.Excerpt))
        {
            throw new DomainException(ErrorKeys.BlogNotPublishable, "excerpt");
        }

        if (string.IsNullOrWhiteSpace(blog.Content))
        {
            throw new DomainException(ErrorKeys.BlogNotPublishable, "content");
        }
    }

    public static void Publish(Blog blog, DateTimeOffset now)
    {
        EnsurePublishable(blog);

        var alreadyLive = BlogVisibility.IsLive(blog.Status, blog.PublishedAt, hasCover: true, now);
        blog.Status = BlogStatuses.Published;
        blog.PublishedAt = alreadyLive ? blog.PublishedAt : now;
    }

    public static void Unpublish(Blog blog)
    {
        blog.Status = BlogStatuses.Draft;
        blog.PublishedAt = null;
    }

    public static void Schedule(Blog blog, DateTimeOffset publishAt, DateTimeOffset now)
    {
        if (publishAt <= now)
        {
            throw new DomainException(ErrorKeys.BlogScheduleMustBeFuture, "publishAt");
        }

        EnsurePublishable(blog);
        blog.Status = BlogStatuses.Scheduled;
        blog.PublishedAt = publishAt.ToUniversalTime();
    }
}
