using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using PsikoAl.Api.Authorization;
using PsikoAl.Api.Extensions;
using PsikoAl.Common.Constants;
using PsikoAl.Common.Dtos.Admin;
using PsikoAl.Common.Dtos.Blog.Create;
using PsikoAl.Common.Dtos.Blog.Update;
using PsikoAl.Common.Exceptions;
using PsikoAl.Services.Abstractions;

namespace PsikoAl.Api.Controllers;

[ApiController]
[Route("admin/blogs")]
[Authorize(Policy = AdminPolicies.Content)]
public sealed class AdminBlogsController(IAdminBlogService adminBlogService) : ControllerBase
{
    private const int MaxCoverBytes = 10 * 1024 * 1024;

    [HttpGet]
    public Task<IReadOnlyList<AdminBlogListItemDto>> List([FromQuery] string? status, CancellationToken cancellationToken)
        => adminBlogService.ListAsync(status, cancellationToken);

    [HttpGet("{id:guid}")]
    public Task<AdminBlogDetailDto> GetDetail(Guid id, CancellationToken cancellationToken)
        => adminBlogService.GetDetailAsync(id, cancellationToken);

    [HttpPost]
    public Task<AdminBlogDetailDto> Create(CreateBlogDto request, CancellationToken cancellationToken)
        => adminBlogService.CreateAsync(this.CurrentUserId(), request, cancellationToken);

    [HttpPut("{id:guid}")]
    public Task<AdminBlogDetailDto> Update(Guid id, UpdateBlogDto request, CancellationToken cancellationToken)
        => adminBlogService.UpdateAsync(this.CurrentUserId(), id, request, cancellationToken);

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await adminBlogService.DeleteAsync(this.CurrentUserId(), id, cancellationToken);
        return Ok(new { success = true });
    }

    [HttpPost("{id:guid}/publish")]
    public Task<AdminBlogDetailDto> Publish(Guid id, CancellationToken cancellationToken)
        => adminBlogService.PublishAsync(this.CurrentUserId(), id, cancellationToken);

    [HttpPost("{id:guid}/unpublish")]
    public Task<AdminBlogDetailDto> Unpublish(Guid id, CancellationToken cancellationToken)
        => adminBlogService.UnpublishAsync(this.CurrentUserId(), id, cancellationToken);

    [HttpPost("{id:guid}/schedule")]
    public Task<AdminBlogDetailDto> Schedule(Guid id, AdminScheduleBlogDto request, CancellationToken cancellationToken)
        => adminBlogService.ScheduleAsync(this.CurrentUserId(), id, request.PublishAt, cancellationToken);

    [HttpPost("{id:guid}/cover")]
    [EnableRateLimiting(RateLimitPolicies.Upload)]
    [RequestSizeLimit(MaxCoverBytes)]
    public async Task<AdminBlogDetailDto> UploadCover(Guid id, IFormFile file, CancellationToken cancellationToken)
    {
        if (file is null or { Length: 0 })
        {
            throw new DomainException(ErrorKeys.ValidationFailed, "file");
        }

        await using var content = file.OpenReadStream();
        return await adminBlogService.SetCoverAsync(this.CurrentUserId(), id, content, file.ContentType, cancellationToken);
    }
}
