using System.Net;

namespace PsikoAl.Api.ContractTests;

public sealed class HealthTests : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client;

    public HealthTests(ApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Health_endpoint_returns_ok()
    {
        var response = await _client.GetAsync("/health", CancellationToken.None);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Whoami_without_token_returns_unauthorized()
    {
        var response = await _client.GetAsync("/auth/whoami", CancellationToken.None);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
