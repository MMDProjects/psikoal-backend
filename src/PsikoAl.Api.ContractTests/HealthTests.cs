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
    public async Task Liveness_endpoint_returns_ok_without_touching_dependencies()
    {
        var response = await _client.GetAsync("/health/live", CancellationToken.None);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    /// <summary>/health artık DB'yi de yokluyor: ulaşılamıyorsa 503, asla boş 500 değil.</summary>
    /// <remarks>
    /// Ortamda veritabanı olup olmaması geliştiricinin user-secrets'ına bağlı; iki
    /// sonuç da geçerli, yalnız "sağlık ucu patladı" (500) geçersiz.
    /// </remarks>
    [Fact]
    public async Task Readiness_endpoint_reports_database_state()
    {
        var response = await _client.GetAsync("/health", CancellationToken.None);
        Assert.Contains(response.StatusCode, new[] { HttpStatusCode.OK, HttpStatusCode.ServiceUnavailable });
    }

    [Fact]
    public async Task Whoami_without_token_returns_unauthorized()
    {
        var response = await _client.GetAsync("/auth/whoami", CancellationToken.None);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
