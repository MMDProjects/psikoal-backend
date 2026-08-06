using System.Net;
using System.Net.Http.Json;

namespace PsikoAl.Api.ContractTests;

public sealed class NotificationContractTests : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client;

    public NotificationContractTests(ApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Notification_endpoints_require_authentication()
    {
        var list = await _client.GetAsync("/notifications", CancellationToken.None);
        var markRead = await _client.PostAsync($"/notifications/{Guid.NewGuid()}/read", null, CancellationToken.None);
        var markAllRead = await _client.PostAsync("/notifications/read-all", null, CancellationToken.None);
        var registerToken = await _client.PostAsJsonAsync(
            "/push-tokens",
            new { token = "ExponentPushToken[test]", platform = "ios" },
            CancellationToken.None);

        Assert.Equal(HttpStatusCode.Unauthorized, list.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, markRead.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, markAllRead.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, registerToken.StatusCode);
    }

    [Fact]
    public async Task Admin_notification_endpoints_require_authentication()
    {
        var templates = await _client.GetAsync("/admin/notification-templates", CancellationToken.None);
        var send = await _client.PostAsJsonAsync(
            "/admin/notifications/send",
            new { targetType = "segment", role = "client", title = "t", body = "b" },
            CancellationToken.None);

        Assert.Equal(HttpStatusCode.Unauthorized, templates.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, send.StatusCode);
    }
}
