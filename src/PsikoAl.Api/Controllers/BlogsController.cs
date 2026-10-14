using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PsikoAl.Api.Extensions;
using PsikoAl.Common.Dtos.Blog;
using PsikoAl.Services.Abstractions;

namespace PsikoAl.Api.Controllers;

[ApiController]
[Route("blogs")]
public sealed class BlogsController(IBlogService blogService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] string? category,
        [FromQuery] int? page,
        [FromQuery] int? perPage,
        [FromQuery] int? limit,
        CancellationToken cancellationToken)
    {
        var filters = new BlogListFilters(category, page, perPage, limit);
        var result = await blogService.ListAsync(filters, this.CurrentUserIdOrNull(), cancellationToken);
        return Ok(new { data = result.Data, meta = new { page = result.Page, total = result.Total, perPage = result.PerPage } });
    }

    [HttpGet("{slug}")]
    public Task<BlogDto> GetBySlug(string slug, CancellationToken cancellationToken)
        => blogService.GetAsync(slug, this.CurrentUserIdOrNull(), cancellationToken);

    [Authorize]
    [HttpPost("{slug}/like")]
    public Task<BlogLikeResultDto> ToggleLike(string slug, CancellationToken cancellationToken)
        => blogService.ToggleLikeAsync(slug, this.CurrentUserId(), cancellationToken);
}
