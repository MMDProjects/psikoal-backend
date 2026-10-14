using System.Net;
using System.Text;
using PsikoAl.Common.Constants;
using PsikoAl.Common.Exceptions;
using PsikoAl.Services.Options;

namespace PsikoAl.Services.Tests.Auth;

// Sözleşme §2: recover çağrısı redirect_to taşır; reset-password recovery token'ıyla
// GoTrue PUT /auth/v1/user çağırır ve geçersiz token 422 RESET_TOKEN_INVALID olur.
public sealed class SupabaseAuthServicePasswordResetTests
{
    [Fact]
    public async Task Recovery_request_carries_configured_redirect()
    {
        var handler = new RecordingHandler(HttpStatusCode.OK, "{}");
        var service = CreateService(handler, "psikoal://reset-password");

        await service.SendPasswordRecoveryAsync("a@example.test", CancellationToken.None);

        var request = Assert.Single(handler.Requests);
        Assert.Equal(
            "/auth/v1/recover?redirect_to=psikoal%3A%2F%2Freset-password",
            request.Uri.PathAndQuery);
    }

    [Fact]
    public async Task Reset_puts_new_password_with_recovery_token()
    {
        var handler = new RecordingHandler(HttpStatusCode.OK, "{}");
        var service = CreateService(handler);

        await service.ResetPasswordAsync("recovery-token", "YeniSifre123", CancellationToken.None);

        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Put, request.Method);
        Assert.Equal("/auth/v1/user", request.Uri.AbsolutePath);
        Assert.Equal("Bearer recovery-token", request.Authorization);
        Assert.Contains("\"password\":\"YeniSifre123\"", request.Body, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized, """{"error_code":"bad_jwt"}""", ErrorKeys.ResetTokenInvalid)]
    [InlineData(HttpStatusCode.Forbidden, """{"error_code":"session_not_found"}""", ErrorKeys.ResetTokenInvalid)]
    [InlineData(HttpStatusCode.UnprocessableEntity, """{"error_code":"weak_password"}""", ErrorKeys.AuthWeakPassword)]
    [InlineData(HttpStatusCode.UnprocessableEntity, """{"error_code":"same_password"}""", ErrorKeys.AuthSamePassword)]
    [InlineData(HttpStatusCode.Unauthorized, "not json", ErrorKeys.ResetTokenInvalid)]
    public async Task Reset_failures_map_to_domain_errors(HttpStatusCode status, string body, string expectedKey)
    {
        var service = CreateService(new RecordingHandler(status, body));

        var exception = await Assert.ThrowsAsync<DomainException>(
            () => service.ResetPasswordAsync("expired", "YeniSifre123", CancellationToken.None));

        Assert.Equal(expectedKey, exception.ErrorKey);
    }

    private static SupabaseAuthService CreateService(RecordingHandler handler, string redirect = "psikoal://reset-password")
        => new(
            new HttpClient(handler) { BaseAddress = new Uri("http://127.0.0.1:54321/") },
            Microsoft.Extensions.Options.Options.Create(new AuthOptions { PasswordResetRedirectUrl = redirect }));

    private sealed record RecordedRequest(HttpMethod Method, Uri Uri, string? Authorization, string Body);

    private sealed class RecordingHandler(HttpStatusCode status, string body) : HttpMessageHandler
    {
        public List<RecordedRequest> Requests { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var content = request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken);
            Requests.Add(new RecordedRequest(
                request.Method,
                request.RequestUri ?? new Uri("http://invalid"),
                request.Headers.Authorization?.ToString(),
                content));
            return new HttpResponseMessage(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };
        }
    }
}
