using PsikoAl.Common.Dtos.Admin;
using PsikoAl.Common.Dtos.Blog.Create;
using PsikoAl.Common.Dtos.Blog.Update;

namespace PsikoAl.Services.Abstractions;

public interface IAdminBlogService
{
    Task<IReadOnlyList<AdminBlogListItemDto>> ListAsync(string? status, CancellationToken cancellationToken);

    Task<AdminBlogDetailDto> GetDetailAsync(Guid blogId, CancellationToken cancellationToken);

    Task<AdminBlogDetailDto> CreateAsync(Guid actorAuthUserId, CreateBlogDto request, CancellationToken cancellationToken);

    Task<AdminBlogDetailDto> UpdateAsync(Guid actorAuthUserId, Guid blogId, UpdateBlogDto request, CancellationToken cancellationToken);

    Task DeleteAsync(Guid actorAuthUserId, Guid blogId, CancellationToken cancellationToken);

    Task<AdminBlogDetailDto> PublishAsync(Guid actorAuthUserId, Guid blogId, CancellationToken cancellationToken);

    Task<AdminBlogDetailDto> UnpublishAsync(Guid actorAuthUserId, Guid blogId, CancellationToken cancellationToken);

    Task<AdminBlogDetailDto> ScheduleAsync(
        Guid actorAuthUserId,
        Guid blogId,
        DateTimeOffset publishAt,
        CancellationToken cancellationToken);

    Task<AdminBlogDetailDto> SetCoverAsync(
        Guid actorAuthUserId,
        Guid blogId,
        Stream content,
        string contentType,
        CancellationToken cancellationToken);
}
