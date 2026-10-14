using PsikoAl.Common.Constants;
using PsikoAl.Common.Dtos.Admin;
using PsikoAl.Common.Dtos.Blog;
using PsikoAl.Data.Entities;
using PsikoAl.Data.Repositories;
using PsikoAl.Services.Abstractions;

namespace PsikoAl.Services.Mapping;

public static class BlogMapper
{
    public static BlogListItemDto ToListItemDto(BlogReadModel blog, ISupabaseStorageService storage)
        => new(
            blog.Id,
            blog.Slug,
            blog.Title,
            blog.Excerpt,
            storage.GetPublicUrl(StorageBuckets.BlogMedia, blog.CoverImagePath),
            CategoriesOf(blog.CategoryBlogTag, blog.Tags),
            blog.LikeCount,
            blog.Liked,
            blog.ReadingMinutes,
            blog.PublishedAt,
            new BlogAuthorDto(blog.AuthorName, blog.AuthorTitle));

    public static BlogDto ToBlogDto(BlogReadModel blog, ISupabaseStorageService storage)
        => new(
            blog.Id,
            blog.Slug,
            blog.Title,
            blog.Excerpt,
            blog.Content,
            storage.GetPublicUrl(StorageBuckets.BlogMedia, blog.CoverImagePath),
            CategoriesOf(blog.CategoryBlogTag, blog.Tags),
            blog.LikeCount,
            blog.Liked,
            blog.ReadingMinutes,
            blog.PublishedAt,
            new BlogAuthorDto(blog.AuthorName, blog.AuthorTitle));

    public static BlogReadModel ToReadModel(Blog blog, int likeCount, bool liked)
        => new(
            blog.Id,
            blog.Slug,
            blog.Title,
            blog.Excerpt,
            blog.Content,
            blog.CoverImagePath ?? string.Empty,
            blog.Category?.BlogTag,
            blog.Tags,
            blog.AuthorName,
            blog.AuthorTitle,
            blog.ReadingMinutes,
            blog.PublishedAt ?? blog.CreatedAt,
            likeCount,
            liked);

    public static AdminBlogDetailDto ToAdminDetailDto(
        Blog blog,
        int likeCount,
        DateTimeOffset now,
        ISupabaseStorageService storage)
        => new(
            blog.Id,
            blog.Slug,
            blog.Title,
            blog.Excerpt,
            blog.Content,
            blog.CoverImagePath is null ? null : storage.GetPublicUrl(StorageBuckets.BlogMedia, blog.CoverImagePath),
            blog.CategoryId,
            blog.Tags,
            blog.AuthorName,
            blog.AuthorTitle,
            blog.ReadingMinutes,
            blog.Status,
            BlogVisibility.IsLive(blog.Status, blog.PublishedAt, blog.CoverImagePath is not null, now),
            blog.PublishedAt,
            likeCount,
            blog.CreatedAt,
            blog.UpdatedAt);

    public static IReadOnlyList<string> CategoriesOf(string? categoryBlogTag, IEnumerable<string> tags)
    {
        var categories = new List<string>();
        if (!string.IsNullOrWhiteSpace(categoryBlogTag))
        {
            categories.Add(categoryBlogTag);
        }

        categories.AddRange(tags.Where(tag => !string.IsNullOrWhiteSpace(tag)));
        return categories.Distinct(StringComparer.Ordinal).ToList();
    }
}
