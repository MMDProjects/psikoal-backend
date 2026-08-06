using System.Net;
using System.Net.Http.Json;

namespace PsikoAl.Api.ContractTests;

public sealed class OfferMatchContractTests : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client;

    public OfferMatchContractTests(ApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Offer_endpoints_require_authentication()
    {
        var my = await _client.GetAsync("/offers/my", CancellationToken.None);
        var detail = await _client.GetAsync($"/offers/{Guid.NewGuid()}", CancellationToken.None);
        var create = await _client.PostAsJsonAsync("/offers", new { listingId = Guid.NewGuid() }, CancellationToken.None);
        var accept = await _client.PostAsync($"/offers/{Guid.NewGuid()}/accept", null, CancellationToken.None);
        var reject = await _client.PostAsync($"/offers/{Guid.NewGuid()}/reject", null, CancellationToken.None);
        var withdraw = await _client.PostAsync($"/offers/{Guid.NewGuid()}/withdraw", null, CancellationToken.None);
        var forListing = await _client.GetAsync($"/listings/{Guid.NewGuid()}/offers", CancellationToken.None);

        Assert.Equal(HttpStatusCode.Unauthorized, my.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, detail.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, create.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, accept.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, reject.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, withdraw.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, forListing.StatusCode);
    }

    [Fact]
    public async Task Match_endpoints_require_authentication()
    {
        var active = await _client.GetAsync("/match/active", CancellationToken.None);
        var list = await _client.GetAsync("/matches", CancellationToken.None);
        var detail = await _client.GetAsync($"/matches/{Guid.NewGuid()}", CancellationToken.None);
        var release = await _client.PostAsJsonAsync($"/match/{Guid.NewGuid()}/release", new { matchId = Guid.NewGuid() }, CancellationToken.None);

        Assert.Equal(HttpStatusCode.Unauthorized, active.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, list.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, detail.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, release.StatusCode);
    }

    [Fact]
    public async Task Admin_match_endpoints_require_authentication()
    {
        var list = await _client.GetAsync("/admin/matches", CancellationToken.None);
        var forceRelease = await _client.PostAsJsonAsync(
            $"/admin/matches/{Guid.NewGuid()}/force-release",
            new { targetStatus = "RELEASED", reason = "test" },
            CancellationToken.None);

        Assert.Equal(HttpStatusCode.Unauthorized, list.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, forceRelease.StatusCode);
    }
}
