using System.Net;
using System.Net.Http.Json;

namespace PsikoAl.Api.ContractTests;

public sealed class CategoryReviewContractTests : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client;

    public CategoryReviewContractTests(ApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Categories_list_does_not_require_authentication()
    {
        var response = await _client.GetAsync("/categories", CancellationToken.None);
        // DB baglantisi olmadigi icin 500 alabilir ama 401 ALMAMALI (public endpoint).
        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Reviews_endpoints_require_authentication()
    {
        var get = await _client.GetAsync($"/experts/{Guid.NewGuid()}/reviews", CancellationToken.None);
        var post = await _client.PostAsJsonAsync(
            $"/experts/{Guid.NewGuid()}/reviews",
            new { rating = 5, comment = "test" },
            CancellationToken.None);

        Assert.Equal(HttpStatusCode.Unauthorized, get.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, post.StatusCode);
    }

    [Fact]
    public async Task Admin_category_and_review_endpoints_require_authentication()
    {
        var categories = await _client.GetAsync("/admin/categories", CancellationToken.None);
        var reviews = await _client.GetAsync("/admin/reviews", CancellationToken.None);

        Assert.Equal(HttpStatusCode.Unauthorized, categories.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, reviews.StatusCode);
    }
}
