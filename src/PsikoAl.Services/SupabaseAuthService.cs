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

    public async Task<LoginResponseDto> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken)
    {
        var response = await httpClient.PostAsJsonAsync(
            "auth/v1/token?grant_type=password",
            new { email = request.Email, password = request.Password },
            JsonOptions,
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw await ToDomainExceptionAsync(response, LoginErrorKeyFor, cancellationToken);
        }

        var session = await ReadSessionAsync(response, cancellationToken);
        return new LoginResponseDto(ToUserDto(session.User), ToTokensDto(session));
    }

    public async Task<LoginResponseDto> RegisterAsync(RegisterRequestDto request, CancellationToken cancellationToken)
    {
        var response = await httpClient.PostAsJsonAsync(
            "auth/v1/signup",
            new
            {
                email = request.Email,
                password = request.Password,
                data = new
                {
                    first_name = request.FirstName,
                    last_name = request.LastName,
                    role = request.Role,
                },
            },
            JsonOptions,
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw await ToDomainExceptionAsync(response, RegisterErrorKeyFor, cancellationToken);
        }

        var session = await ReadSessionAsync(response, cancellationToken);
        return new LoginResponseDto(ToUserDto(session.User), ToTokensDto(session));
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

    private static async Task<DomainException> ToDomainExceptionAsync(
        HttpResponseMessage response,
        Func<GoTrueError, string?> mapErrorCode,
        CancellationToken cancellationToken)
    {
        var error = await response.Content.ReadFromJsonAsync<GoTrueError>(JsonOptions, cancellationToken);
        var field = error?.ErrorCode is "email_address_invalid" or "email_exists" or "user_already_exists" ? "email" : null;
        var errorKey = (error is null ? null : mapErrorCode(error)) ?? ErrorKeys.AuthProviderError;
        return new DomainException(errorKey, field);
    }

    private static string? LoginErrorKeyFor(GoTrueError error)
        => error.ErrorCode switch
        {
            "invalid_credentials" or "invalid_grant" => ErrorKeys.AuthInvalidCredentials,
            "email_not_confirmed" => ErrorKeys.AuthEmailNotConfirmed,
            "over_email_send_rate_limit" or "over_request_rate_limit" => ErrorKeys.AuthRateLimited,
            _ => null,
        };

    private static string? RegisterErrorKeyFor(GoTrueError error)
        => error.ErrorCode switch
        {
            "email_exists" or "user_already_exists" => ErrorKeys.AuthEmailAlreadyRegistered,
            "email_address_invalid" => ErrorKeys.AuthEmailInvalid,
            "weak_password" => ErrorKeys.AuthWeakPassword,
            "over_email_send_rate_limit" or "over_request_rate_limit" => ErrorKeys.AuthRateLimited,
            _ => null,
        };

    private static async Task<GoTrueSession> ReadSessionAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var session = await response.Content.ReadFromJsonAsync<GoTrueSession>(JsonOptions, cancellationToken);
        return session ?? throw new DomainException(ErrorKeys.InternalError);
    }

    private static AuthTokensDto ToTokensDto(GoTrueSession session)
        => new(session.AccessToken, session.RefreshToken, session.ExpiresIn);

    private static AuthUserDto ToUserDto(GoTrueUser user)
        => new(
            user.Id,
            user.Email,
            user.UserMetadata.FirstName ?? string.Empty,
            user.UserMetadata.LastName ?? string.Empty,
            user.UserMetadata.Role ?? "client",
            user.EmailConfirmedAt is not null,
            user.UserMetadata.AvatarUrl,
            user.CreatedAt,
            user.UserMetadata.Phone,
            user.UserMetadata.City);

    private sealed record GoTrueError(
        [property: JsonPropertyName("error_code")] string? ErrorCode,
        [property: JsonPropertyName("msg")] string? Msg);

    private sealed record GoTrueSession(
        [property: JsonPropertyName("access_token")] string AccessToken,
        [property: JsonPropertyName("refresh_token")] string RefreshToken,
        [property: JsonPropertyName("expires_in")] int ExpiresIn,
        [property: JsonPropertyName("user")] GoTrueUser User);

    private sealed record GoTrueUser(
        [property: JsonPropertyName("id")] Guid Id,
        [property: JsonPropertyName("email")] string Email,
        [property: JsonPropertyName("email_confirmed_at")] DateTimeOffset? EmailConfirmedAt,
        [property: JsonPropertyName("created_at")] DateTimeOffset CreatedAt,
        [property: JsonPropertyName("user_metadata")] GoTrueUserMetadata UserMetadata);

    private sealed record GoTrueUserMetadata(
        [property: JsonPropertyName("first_name")] string? FirstName,
        [property: JsonPropertyName("last_name")] string? LastName,
        [property: JsonPropertyName("role")] string? Role,
        [property: JsonPropertyName("avatar_url")] string? AvatarUrl,
        [property: JsonPropertyName("phone")] string? Phone,
        [property: JsonPropertyName("city")] string? City);
}
