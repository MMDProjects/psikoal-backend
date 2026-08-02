using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using PsikoAl.Common.Constants;
using PsikoAl.Common.Dtos.Auth;
using PsikoAl.Common.Dtos.Auth.Create;
using PsikoAl.Common.Exceptions;
using PsikoAl.Services.Abstractions;

namespace PsikoAl.Services;

public sealed class SupabaseAuthService(HttpClient httpClient) : ISupabaseAuthService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<SupabaseSessionDto> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken)
    {
        var response = await httpClient.PostAsJsonAsync(
            "auth/v1/token?grant_type=password",
            new { email = request.Email, password = request.Password },
            JsonOptions,
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var error = await ReadErrorAsync(response, cancellationToken);
            throw error?.ErrorCode switch
            {
                "user_banned" => new DomainException(ErrorKeys.AuthAccountFrozen),
                "email_not_confirmed" => new DomainException(ErrorKeys.AuthEmailNotConfirmed),
                "over_email_send_rate_limit" or "over_request_rate_limit" => new DomainException(ErrorKeys.AuthRateLimited),
                _ => new DomainException(ErrorKeys.AuthInvalidCredentials),
            };
        }

        var session = await ReadSessionAsync(response, cancellationToken);
        return new SupabaseSessionDto(session.User.Id, ToTokensDto(session));
    }

    public async Task<AuthTokensDto> RefreshAsync(RefreshRequestDto request, CancellationToken cancellationToken)
    {
        var response = await httpClient.PostAsJsonAsync(
            "auth/v1/token?grant_type=refresh_token",
            new { refresh_token = request.RefreshToken },
            JsonOptions,
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new DomainException(ErrorKeys.AuthRefreshTokenInvalid);
        }

        var session = await ReadSessionAsync(response, cancellationToken);
        return ToTokensDto(session);
    }

    public async Task LogoutAsync(string accessToken, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "auth/v1/logout");
        request.Headers.Authorization = new("Bearer", accessToken);
        await httpClient.SendAsync(request, cancellationToken);
    }

    public async Task SendPasswordRecoveryAsync(string email, CancellationToken cancellationToken)
    {
        await httpClient.PostAsJsonAsync("auth/v1/recover", new { email }, JsonOptions, cancellationToken);
    }

    private static async Task<GoTrueError?> ReadErrorAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        try
        {
            return await response.Content.ReadFromJsonAsync<GoTrueError>(JsonOptions, cancellationToken);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static async Task<GoTrueSession> ReadSessionAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var session = await response.Content.ReadFromJsonAsync<GoTrueSession>(JsonOptions, cancellationToken);
        return session ?? throw new DomainException(ErrorKeys.InternalError);
    }

    private static AuthTokensDto ToTokensDto(GoTrueSession session)
        => new(session.AccessToken, session.RefreshToken, session.ExpiresIn);

    private sealed record GoTrueError(
        [property: JsonPropertyName("error_code")] string? ErrorCode,
        [property: JsonPropertyName("msg")] string? Msg);

    private sealed record GoTrueSession(
        [property: JsonPropertyName("access_token")] string AccessToken,
        [property: JsonPropertyName("refresh_token")] string RefreshToken,
        [property: JsonPropertyName("expires_in")] int ExpiresIn,
        [property: JsonPropertyName("user")] GoTrueUser User);

    private sealed record GoTrueUser([property: JsonPropertyName("id")] Guid Id);
}
