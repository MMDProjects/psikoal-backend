using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PsikoAl.Api.Extensions;
using PsikoAl.Services.Abstractions;

namespace PsikoAl.Api.Controllers;

// Mobil uygulama YALNIZ bakiye, geçmiş ve teklif maliyetini gösterir; satın alma
// yolu yoktur (App Store / Play kuralı). Paket listesi web sitesinin ödeme sayfası içindir.
[ApiController]
[Route("credits")]
public sealed class CreditsController(ICreditService creditService) : ControllerBase
{
    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> GetMine(CancellationToken cancellationToken)
        => Ok(new { data = await creditService.GetMyBalanceAsync(this.CurrentUserId(), cancellationToken) });

    [Authorize]
    [HttpGet("transactions")]
    public async Task<IActionResult> ListMyTransactions(
        [FromQuery] int? page,
        [FromQuery] int? perPage,
        CancellationToken cancellationToken)
    {
        var result = await creditService.ListMyTransactionsAsync(this.CurrentUserId(), page, perPage, cancellationToken);
        return Ok(new { data = result.Data, meta = new { page = result.Page, total = result.Total, perPage = result.PerPage } });
    }

    [AllowAnonymous]
    [HttpGet("packages")]
    public async Task<IActionResult> ListPackages(CancellationToken cancellationToken)
        => Ok(new { data = await creditService.ListActivePackagesAsync(cancellationToken) });
}
