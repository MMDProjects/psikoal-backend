using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PsikoAl.Api.Extensions;
using PsikoAl.Common.Dtos.Listing;
using PsikoAl.Common.Dtos.Listing.Create;
using PsikoAl.Services.Abstractions;

namespace PsikoAl.Api.Controllers;

[ApiController]
[Route("listings")]
[Authorize]
public sealed class ListingsController(IListingService listingService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> ListFeed(
        [FromQuery] string[]? specialization,
        [FromQuery] string[]? sessionType,
        [FromQuery] decimal? budgetMin,
        [FromQuery] decimal? budgetMax,
        [FromQuery] string? sort,
        CancellationToken cancellationToken)
    {
        var filters = new ListingFeedFilters(specialization, sessionType, budgetMin, budgetMax, sort);
        var result = await listingService.ListFeedAsync(filters, cancellationToken);
        return Ok(new { data = result.Data, meta = new { page = 1, total = result.Total, perPage = result.Total } });
    }

    [HttpGet("my")]
    public async Task<IActionResult> ListMy([FromQuery] string? status, CancellationToken cancellationToken)
    {
        var result = await listingService.ListMyAsync(this.CurrentUserId(), status, cancellationToken);
        return Ok(new
        {
            data = result.Data,
            meta = new { page = 1, total = result.Total, perPage = result.Total, activeCount = result.ActiveCount },
        });
    }

    [HttpGet("{id:guid}")]
    public Task<ListingDto> GetById(Guid id, CancellationToken cancellationToken)
        => listingService.GetByIdAsync(id, this.CurrentUserId(), cancellationToken);

    [HttpPost]
    public Task<ListingDto> Create(CreateListingDto request, CancellationToken cancellationToken)
        => listingService.CreateAsync(this.CurrentUserId(), request, cancellationToken);

    [HttpPost("{id:guid}/close")]
    public Task<ListingDto> Close(Guid id, CancellationToken cancellationToken)
        => listingService.CloseAsync(this.CurrentUserId(), id, cancellationToken);
}
