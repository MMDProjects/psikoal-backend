using Microsoft.EntityFrameworkCore;
using PsikoAl.Common.Constants;
using PsikoAl.Common.Dtos.Blog;
using PsikoAl.Common.Exceptions;
using PsikoAl.Common.Text;
using PsikoAl.Data.Entities;
using PsikoAl.Data.Repositories;
using PsikoAl.Data.Repositories.Abstractions;
using PsikoAl.Services.Abstractions;
using PsikoAl.Services.Mapping;

namespace PsikoAl.Services;

public sealed class BlogService(
    IUnitOfWork unitOfWork,
    ISupabaseStorageService storage,
    TimeProvider timeProvider) : IBlogService
{
    public async Task<BlogListResult> ListAsync(
        BlogListFilters filters,
        Guid? viewerId,
        CancellationToken cancellationToken)
    {
        var page = filters.EffectivePage;
        var perPage = filters.EffectivePerPage;
        var tag = BlogTagNormalizer.NormalizeOne(filters.Category);

        var visible = unitOfWork.Blogs.Query()
            .Where(BlogVisibility.IsPublicAt(timeProvider.GetUtcNow()))
            .WithTag(tag);

        var total = await visible.CountAsync(cancellationToken);
        var rows = await visible
            .OrderByDescending(blog => blog.PublishedAt)
            .ThenByDescending(blog => blog.CreatedAt)
            .Skip((page - 1) * perPage)
            .Take(perPage)
            .Select(blog => new BlogReadModel(
                blog.Id,
                blog.Slug,
                blog.Title,
                blog.Excerpt,
                string.Empty,
                blog.CoverImagePath ?? string.Empty,
                blog.Category != null ? blog.Category.BlogTag : null,
                blog.Tags,
                blog.AuthorName,
                blog.AuthorTitle,
                blog.ReadingMinutes,
                blog.PublishedAt ?? blog.CreatedAt,
                blog.Likes.Count,
                viewerId != null && blog.Likes.Any(like => like.UserId == viewerId)))
            .ToListAsync(cancellationToken);

        var items = rows.Select(row => BlogMapper.ToListItemDto(row, storage)).ToList();
        return new BlogListResult(items, page, perPage, total);
    }

    public async Task<BlogDto> GetAsync(string slugOrId, Guid? viewerId, CancellationToken cancellationToken)
    {
        var blog = await GetRequiredPublicBlogAsync(slugOrId, cancellationToken);
        var likeCount = await unitOfWork.BlogLikes.CountForBlogAsync(blog.Id, cancellationToken);
        var liked = viewerId.HasValue
            && await unitOfWork.BlogLikes.FindAsync(blog.Id, viewerId.Value, cancellationToken) is not null;

        return BlogMapper.ToBlogDto(BlogMapper.ToReadModel(blog, likeCount, liked), storage);
    }

    public async Task<BlogLikeResultDto> ToggleLikeAsync(
        string slugOrId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var blog = await GetRequiredPublicBlogAsync(slugOrId, cancellationToken);

        if (!await unitOfWork.Profiles.ExistsAsync(userId, cancellationToken))
        {
            throw new DomainException(ErrorKeys.ProfileNotFound);
        }

        var existing = await unitOfWork.BlogLikes.FindAsync(blog.Id, userId, cancellationToken);
        var liked = existing is null;
        if (existing is null)
        {
            await unitOfWork.BlogLikes.AddAsync(new BlogLike { BlogId = blog.Id, UserId = userId }, cancellationToken);
        }
        else
        {
            unitOfWork.BlogLikes.Delete(existing);
        }

        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException) when (liked)
        {
            // Eş zamanlı iki "beğen" isteğinden ikincisi birincil anahtara çarpar:
            // kullanıcı zaten beğenmiş durumdadır, sonuç aynıdır.
        }

        var likeCount = await unitOfWork.BlogLikes.CountForBlogAsync(blog.Id, cancellationToken);
        return new BlogLikeResultDto(likeCount, liked);
    }

    private async Task<Blog> GetRequiredPublicBlogAsync(string slugOrId, CancellationToken cancellationToken)
        => await unitOfWork.Blogs.GetPublicAsync(slugOrId, timeProvider.GetUtcNow(), cancellationToken)
            ?? throw new DomainException(ErrorKeys.BlogNotFound);
}
