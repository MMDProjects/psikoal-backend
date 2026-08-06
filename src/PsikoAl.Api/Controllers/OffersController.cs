using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PsikoAl.Api.Extensions;
using PsikoAl.Common.Dtos.Offer;
using PsikoAl.Common.Dtos.Offer.Create;
using PsikoAl.Services.Abstractions;

namespace PsikoAl.Api.Controllers;

[ApiController]
[Route("offers")]
[Authorize]
public sealed class OffersController(IOfferService offerService) : ControllerBase
{
    [HttpGet("my")]
    public async Task<IActionResult> ListMy([FromQuery] string? status, CancellationToken cancellationToken)
    {
        var result = await offerService.ListMyAsync(this.CurrentUserId(), status, cancellationToken);
        return Ok(new
        {
            data = result.Data,
            meta = new { page = 1, total = result.Total, perPage = result.Total, pendingCount = result.PendingCount },
        });
    }

    [HttpGet("{id:guid}")]
    public Task<OfferDto> GetById(Guid id, CancellationToken cancellationToken)
        => offerService.GetByIdAsync(id, this.CurrentUserId(), cancellationToken);

    [HttpPost]
    public Task<OfferDto> Create(CreateOfferDto request, CancellationToken cancellationToken)
        => offerService.CreateAsync(this.CurrentUserId(), request, cancellationToken);

    [HttpPost("{id:guid}/accept")]
    public Task<OfferDto> Accept(Guid id, CancellationToken cancellationToken)
        => offerService.AcceptAsync(this.CurrentUserId(), id, cancellationToken);

    [HttpPost("{id:guid}/reject")]
    public Task<OfferDto> Reject(Guid id, CancellationToken cancellationToken)
        => offerService.RejectAsync(this.CurrentUserId(), id, cancellationToken);

    [HttpPost("{id:guid}/withdraw")]
    public Task<OfferDto> Withdraw(Guid id, CancellationToken cancellationToken)
        => offerService.WithdrawAsync(this.CurrentUserId(), id, cancellationToken);
}
