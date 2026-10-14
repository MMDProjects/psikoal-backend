using PsikoAl.Common.Dtos.Blog;

namespace PsikoAl.Services.Abstractions;

public interface IBlogService
{
    Task<BlogListResult> ListAsync(BlogListFilters filters, Guid? viewerId, CancellationToken cancellationToken);

    Task<BlogDto> GetAsync(string slugOrId, Guid? viewerId, CancellationToken cancellationToken);

    Task<BlogLikeResultDto> ToggleLikeAsync(string slugOrId, Guid userId, CancellationToken cancellationToken);
}
