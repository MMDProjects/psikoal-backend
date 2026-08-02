using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace PsikoAl.Api.ContractTests;

public sealed class AuthContractTests : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client;

    public AuthContractTests(ApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Register_with_invalid_role_returns_bad_request()
    {
        var response = await _client.PostAsJsonAsync("/auth/register", new
        {
            email = "contract@test.com",
            password = "TestPass123!",
            firstName = "Contract",
            lastName = "Test",
            role = "superuser",
        }, CancellationToken.None);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_with_short_password_returns_bad_request()
    {
        var response = await _client.PostAsJsonAsync("/auth/register", new
        {
            email = "contract@test.com",
            password = "short",
            firstName = "Contract",
            lastName = "Test",
            role = "client",
        }, CancellationToken.None);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Forgot_password_always_returns_success_envelope()
    {
        var response = await _client.PostAsJsonAsync(
            "/auth/forgot-password",
            new { email = "unknown@test.com" },
            CancellationToken.None);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(CancellationToken.None);
        Assert.True(body.GetProperty("success").GetBoolean());
    }

    [Fact]
    public async Task Me_endpoints_require_authentication()
    {
        var get = await _client.GetAsync("/auth/me", CancellationToken.None);
        var patch = await _client.PatchAsJsonAsync("/auth/me", new { firstName = "X" }, CancellationToken.None);
        var delete = await _client.DeleteAsync("/auth/me", CancellationToken.None);
        var freeze = await _client.PostAsync("/auth/freeze", null, CancellationToken.None);

        Assert.Equal(HttpStatusCode.Unauthorized, get.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, patch.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, delete.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, freeze.StatusCode);
    }

    [Fact]
    public async Task Admin_users_requires_authentication()
    {
        var response = await _client.GetAsync("/admin/users", CancellationToken.None);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
