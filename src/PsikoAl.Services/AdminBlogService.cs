using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PsikoAl.Common.Constants;
using PsikoAl.Common.Dtos.Admin;
using PsikoAl.Common.Dtos.Blog.Create;
using PsikoAl.Common.Dtos.Blog.Update;
using PsikoAl.Common.Exceptions;
using PsikoAl.Common.Presentation;
using PsikoAl.Common.Text;
using PsikoAl.Data.Entities;
using PsikoAl.Data.Repositories;
using PsikoAl.Data.Repositories.Abstractions;
using PsikoAl.Services.Abstractions;
using PsikoAl.Services.Mapping;

namespace PsikoAl.Services;

public sealed class AdminBlogService(
    IUnitOfWork unitOfWork,
    IAdminGuard adminGuard,
    IUploadService uploadService,
    ISupabaseStorageService storage,
    TimeProvider timeProvider) : IAdminBlogService
{
    private const string AuditEntityType = "blog";

    public async Task<IReadOnlyList<AdminBlogListItemDto>> ListAsync(string? status, CancellationToken cancellationToken)
    {
        var query = unitOfWork.Blogs.Query();
        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(blog => blog.Status == status);
        }

        var rows = await query
            .OrderByDescending(blog => blog.UpdatedAt)
            .Select(blog => new
            {
                blog.Id,
                blog.Slug,
                blog.Title,
                blog.Status,
                CategoryName = blog.Category != null ? blog.Category.Name : null,
                blog.PublishedAt,
                LikeCount = blog.Likes.Count,
                HasCover = blog.CoverImagePath != null,
                blog.UpdatedAt,
            })
            .ToListAsync(cancellationToken);

        var now = timeProvider.GetUtcNow();
        return rows
            .Select(row => new AdminBlogListItemDto(
                row.Id,
                row.Slug,
                row.Title,
                row.Status,
                BlogVisibility.IsLive(row.Status, row.PublishedAt, row.HasCover, now),
                row.CategoryName,
                row.PublishedAt,
                row.LikeCount,
                row.HasCover,
                row.UpdatedAt))
            .ToList();
    }

    public async Task<AdminBlogDetailDto> GetDetailAsync(Guid blogId, CancellationToken cancellationToken)
        => await ToDetailAsync(await GetRequiredBlogAsync(blogId, cancellationToken), cancellationToken);

    public async Task<AdminBlogDetailDto> CreateAsync(
        Guid actorAuthUserId,
        CreateBlogDto request,
        CancellationToken cancellationToken)
    {
        var actor = await GetRequiredActorAsync(actorAuthUserId, cancellationToken);
        var slug = string.IsNullOrWhiteSpace(request.Slug) ? SlugGenerator.FromTitle(request.Title) : request.Slug.Trim();
        await EnsureSlugAvailableAsync(slug, null, cancellationToken);
        await EnsureCategoryExistsAsync(request.CategoryId, cancellationToken);

        var blog = new Blog
        {
            Slug = slug,
            Title = request.Title.Trim(),
            Excerpt = request.Excerpt.Trim(),
            Content = request.Content.Trim(),
            CategoryId = request.CategoryId,
            Tags = [.. BlogTagNormalizer.Normalize(request.Tags)],
            AuthorName = request.AuthorName.Trim(),
            AuthorTitle = request.AuthorTitle.Trim(),
            ReadingMinutes = ReadingTimeCalculator.MinutesFor(request.Content),
            Status = BlogStatuses.Draft,
            CreatedBy = actor.Id,
        };

        await unitOfWork.Blogs.AddAsync(blog, cancellationToken);
        await AddAuditAsync(actor.Id, "admin.blog_create", blog, null, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return await ToDetailAsync(blog, cancellationToken);
    }

    public async Task<AdminBlogDetailDto> UpdateAsync(
        Guid actorAuthUserId,
        Guid blogId,
        UpdateBlogDto request,
        CancellationToken cancellationToken)
    {
        var actor = await GetRequiredActorAsync(actorAuthUserId, cancellationToken);
        var blog = await GetRequiredBlogAsync(blogId, cancellationToken);
        var oldValue = Snapshot(blog);

        var slug = request.Slug.Trim();
        await EnsureSlugAvailableAsync(slug, blog.Id, cancellationToken);
        await EnsureCategoryExistsAsync(request.CategoryId, cancellationToken);

        blog.Slug = slug;
        blog.Title = request.Title.Trim();
        blog.Excerpt = request.Excerpt.Trim();
        blog.Content = request.Content.Trim();
        blog.CategoryId = request.CategoryId;
        blog.Tags = [.. BlogTagNormalizer.Normalize(request.Tags)];
        blog.AuthorName = request.AuthorName.Trim();
        blog.AuthorTitle = request.AuthorTitle.Trim();
        blog.ReadingMinutes = ReadingTimeCalculator.MinutesFor(request.Content);

        if (blog.Status != BlogStatuses.Draft)
        {
            BlogPublishRules.EnsurePublishable(blog);
        }

        await AddAuditAsync(actor.Id, "admin.blog_update", blog, oldValue, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return await ToDetailAsync(blog, cancellationToken);
    }

    public async Task DeleteAsync(Guid actorAuthUserId, Guid blogId, CancellationToken cancellationToken)
    {
        var actor = await GetRequiredActorAsync(actorAuthUserId, cancellationToken);
        var blog = await GetRequiredBlogAsync(blogId, cancellationToken);

        await unitOfWork.AuditLogs.AddAsync(
            new AuditLog
            {
                AdminUserId = actor.Id,
                ActorType = AuditActorTypes.Admin,
                Action = "admin.blog_delete",
                EntityType = AuditEntityType,
                EntityId = blog.Id.ToString(),
                OldValue = Snapshot(blog),
            },
            cancellationToken);
        unitOfWork.Blogs.Delete(blog);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        await TryDeleteStorageAsync(() => storage.DeleteFolderAsync(StorageBuckets.BlogMedia, blog.Id.ToString(), cancellationToken));
    }

    public async Task<AdminBlogDetailDto> PublishAsync(Guid actorAuthUserId, Guid blogId, CancellationToken cancellationToken)
    {
        var actor = await GetRequiredActorAsync(actorAuthUserId, cancellationToken);
        var blog = await GetRequiredBlogAsync(blogId, cancellationToken);
        var oldValue = Snapshot(blog);

        BlogPublishRules.Publish(blog, timeProvider.GetUtcNow());

        await AddAuditAsync(actor.Id, "admin.blog_publish", blog, oldValue, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return await ToDetailAsync(blog, cancellationToken);
    }

    public async Task<AdminBlogDetailDto> UnpublishAsync(Guid actorAuthUserId, Guid blogId, CancellationToken cancellationToken)
    {
        var actor = await GetRequiredActorAsync(actorAuthUserId, cancellationToken);
        var blog = await GetRequiredBlogAsync(blogId, cancellationToken);
        var oldValue = Snapshot(blog);

        BlogPublishRules.Unpublish(blog);

        await AddAuditAsync(actor.Id, "admin.blog_unpublish", blog, oldValue, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return await ToDetailAsync(blog, cancellationToken);
    }

    public async Task<AdminBlogDetailDto> ScheduleAsync(
        Guid actorAuthUserId,
        Guid blogId,
        DateTimeOffset publishAt,
        CancellationToken cancellationToken)
    {
        var actor = await GetRequiredActorAsync(actorAuthUserId, cancellationToken);
        var blog = await GetRequiredBlogAsync(blogId, cancellationToken);
        var oldValue = Snapshot(blog);

        BlogPublishRules.Schedule(blog, publishAt, timeProvider.GetUtcNow());

        await AddAuditAsync(actor.Id, "admin.blog_schedule", blog, oldValue, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return await ToDetailAsync(blog, cancellationToken);
    }

    public async Task<AdminBlogDetailDto> SetCoverAsync(
        Guid actorAuthUserId,
        Guid blogId,
        Stream content,
        string contentType,
        CancellationToken cancellationToken)
    {
        var actor = await GetRequiredActorAsync(actorAuthUserId, cancellationToken);
        var blog = await GetRequiredBlogAsync(blogId, cancellationToken);
        var oldValue = Snapshot(blog);
        var previousCoverPath = blog.CoverImagePath;

        blog.CoverImagePath = await uploadService.UploadBlogCoverAsync(blog.Id, content, contentType, cancellationToken);

        await AddAuditAsync(actor.Id, "admin.blog_cover", blog, oldValue, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        if (previousCoverPath is not null && previousCoverPath != blog.CoverImagePath)
        {
            await TryDeleteStorageAsync(() => storage.DeleteObjectAsync(StorageBuckets.BlogMedia, previousCoverPath, cancellationToken));
        }

        return await ToDetailAsync(blog, cancellationToken);
    }

    // Kayıt zaten kaydedildi; Storage temizliği başarısız olursa yalnız sahipsiz bir
    // görsel kalır. Admin işlemini bu yüzden düşürmek yanlış olur.
    private static async Task TryDeleteStorageAsync(Func<Task> delete)
    {
        try
        {
            await delete();
        }
        catch (DomainException)
        {
        }
    }

    private async Task EnsureSlugAvailableAsync(string slug, Guid? exceptBlogId, CancellationToken cancellationToken)
    {
        if (!SlugGenerator.IsValid(slug))
        {
            throw new DomainException(ErrorKeys.BlogSlugInvalid, "slug");
        }

        if (await unitOfWork.Blogs.SlugExistsAsync(slug, exceptBlogId, cancellationToken))
        {
            throw new DomainException(ErrorKeys.BlogSlugAlreadyExists, "slug");
        }
    }

    private async Task EnsureCategoryExistsAsync(Guid? categoryId, CancellationToken cancellationToken)
    {
        if (categoryId.HasValue && !await unitOfWork.Categories.ExistsAsync(categoryId.Value, cancellationToken))
        {
            throw new DomainException(ErrorKeys.CategoryNotFound, "categoryId");
        }
    }

    private async Task<AdminBlogDetailDto> ToDetailAsync(Blog blog, CancellationToken cancellationToken)
    {
        var likeCount = await unitOfWork.BlogLikes.CountForBlogAsync(blog.Id, cancellationToken);
        return BlogMapper.ToAdminDetailDto(blog, likeCount, timeProvider.GetUtcNow(), storage);
    }

    private async Task<Blog> GetRequiredBlogAsync(Guid blogId, CancellationToken cancellationToken)
        => await unitOfWork.Blogs.GetByIdAsync(blogId, cancellationToken)
            ?? throw new DomainException(ErrorKeys.BlogNotFound);

    private async Task<AdminUser> GetRequiredActorAsync(Guid actorAuthUserId, CancellationToken cancellationToken)
        => await adminGuard.GetActiveAdminAsync(actorAuthUserId, cancellationToken)
            ?? throw new DomainException(ErrorKeys.AdminUserNotFound);

    private static string Snapshot(Blog blog)
        => JsonSerializer.Serialize(new
        {
            blog.Slug,
            blog.Title,
            blog.Status,
            blog.PublishedAt,
            blog.CategoryId,
            blog.Tags,
            blog.CoverImagePath,
            blog.AuthorName,
        });

    private async Task AddAuditAsync(
        Guid adminUserId,
        string action,
        Blog blog,
        string? oldValue,
        CancellationToken cancellationToken)
        => await unitOfWork.AuditLogs.AddAsync(
            new AuditLog
            {
                AdminUserId = adminUserId,
                ActorType = AuditActorTypes.Admin,
                Action = action,
                EntityType = AuditEntityType,
                EntityId = blog.Id.ToString(),
                OldValue = oldValue,
                NewValue = Snapshot(blog),
            },
            cancellationToken);
}
