using System.Net;
using System.Net.Http.Json;

namespace PsikoAl.Api.ContractTests;

public sealed class ListingContractTests : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client;

    public ListingContractTests(ApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Listing_endpoints_require_authentication()
    {
        var feed = await _client.GetAsync("/listings", CancellationToken.None);
        var mine = await _client.GetAsync("/listings/my", CancellationToken.None);
        var detail = await _client.GetAsync($"/listings/{Guid.NewGuid()}", CancellationToken.None);
        var create = await _client.PostAsJsonAsync("/listings", new { title = "test" }, CancellationToken.None);
        var close = await _client.PostAsync($"/listings/{Guid.NewGuid()}/close", null, CancellationToken.None);

        Assert.Equal(HttpStatusCode.Unauthorized, feed.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, mine.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, detail.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, create.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, close.StatusCode);
    }

    [Fact]
    public async Task Admin_listing_and_system_settings_endpoints_require_authentication()
    {
        var listings = await _client.GetAsync("/admin/listings", CancellationToken.None);
        var settings = await _client.GetAsync("/admin/system-settings", CancellationToken.None);

        Assert.Equal(HttpStatusCode.Unauthorized, listings.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, settings.StatusCode);
    }
}
