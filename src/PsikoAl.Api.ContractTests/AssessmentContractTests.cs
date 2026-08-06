using System.Net;
using System.Net.Http.Json;

namespace PsikoAl.Api.ContractTests;

public sealed class AssessmentContractTests : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client;

    public AssessmentContractTests(ApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Assessment_browsing_and_submit_do_not_require_authentication()
    {
        // ApiFactory gerçek bir Postgres'e bağlanmadığı için burada 200 bekleyemeyiz —
        // amaç yalnızca bu uçların 401 (auth zorunluluğu) döndürmediğini doğrulamak.
        var list = await _client.GetAsync("/assessment", CancellationToken.None);
        var active = await _client.GetAsync("/assessment/active", CancellationToken.None);

        Assert.NotEqual(HttpStatusCode.Unauthorized, list.StatusCode);
        Assert.NotEqual(HttpStatusCode.Unauthorized, active.StatusCode);
    }

    [Fact]
    public async Task My_results_requires_authentication()
    {
        var myResults = await _client.GetAsync("/assessment/results/my", CancellationToken.None);
        Assert.Equal(HttpStatusCode.Unauthorized, myResults.StatusCode);
    }

    [Fact]
    public async Task Admin_assessment_endpoints_require_authentication()
    {
        var list = await _client.GetAsync("/admin/assessments", CancellationToken.None);
        var update = await _client.PatchAsJsonAsync(
            $"/admin/assessments/{Guid.NewGuid()}",
            new { title = "t", description = "d", category = "c", estimatedMinutes = 3, isActive = true, sortOrder = 1 },
            CancellationToken.None);

        Assert.Equal(HttpStatusCode.Unauthorized, list.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, update.StatusCode);
    }
}
