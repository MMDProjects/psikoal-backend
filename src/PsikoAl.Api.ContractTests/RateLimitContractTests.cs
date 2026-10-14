using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using PsikoAl.Common.Constants;

namespace PsikoAl.Api.ContractTests;

public sealed class RateLimitContractTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    /// <summary>Şifre sıfırlama "auth" limitine tabi ve ret gövdesi ApiErrorDto.</summary>
    [Fact]
    public async Task Reset_password_is_rate_limited_with_api_error_body()
    {
        var client = factory.CreateClient();
        HttpResponseMessage? last = null;

        for (var attempt = 0; attempt < 11; attempt++)
        {
            last = await client.PostAsJsonAsync(
                "/auth/reset-password",
                new { accessToken = string.Empty, newPassword = "x" },
                CancellationToken.None);
        }

        Assert.NotNull(last);
        Assert.Equal(HttpStatusCode.TooManyRequests, last.StatusCode);
        var body = await last.Content.ReadFromJsonAsync<JsonElement>(CancellationToken.None);
        Assert.Equal(ErrorKeys.AuthRateLimited, body.GetProperty("code").GetString());
    }

    /// <summary>Refresh her istemcide sık çağrılır; "auth" limitinin 10/dk'sına takılmamalı.</summary>
    [Fact]
    public async Task Refresh_is_not_throttled_like_credential_endpoints()
    {
        var client = factory.CreateClient();

        for (var attempt = 0; attempt < 15; attempt++)
        {
            var response = await client.PostAsJsonAsync(
                "/auth/refresh",
                new { refreshToken = string.Empty },
                CancellationToken.None);
            Assert.NotEqual(HttpStatusCode.TooManyRequests, response.StatusCode);
        }
    }
}
