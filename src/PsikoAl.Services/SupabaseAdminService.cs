using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using PsikoAl.Common.Constants;
using PsikoAl.Common.Exceptions;
using PsikoAl.Services.Abstractions;

namespace PsikoAl.Services;

public sealed class SupabaseAdminService(HttpClient httpClient) : ISupabaseAdminService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<Guid> CreateConfirmedUserAsync(
        string email,
        string password,
        string firstName,
        string lastName,
        string role,
        CancellationToken cancellationToken)
    {
        var response = await httpClient.PostAsJsonAsync(
            "auth/v1/admin/users",
            new
            {
                email,
                password,
                email_confirm = true,
                user_metadata = new
                {
                    first_name = firstName,
                    last_name = lastName,
                    role,
                },
            },
            JsonOptions,
            cancellationToken);

        if (response.StatusCode is HttpStatusCode.UnprocessableEntity or HttpStatusCode.Conflict)
        {
            throw new DomainException(ErrorKeys.AuthEmailAlreadyRegistered, "email");
        }

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadFromJsonAsync<GoTrueError>(JsonOptions, cancellationToken);
            throw error?.ErrorCode switch
            {
                "email_exists" or "user_already_exists" => new DomainException(ErrorKeys.AuthEmailAlreadyRegistered, "email"),
                "email_address_invalid" => new DomainException(ErrorKeys.AuthEmailInvalid, "email"),
                "weak_password" => new DomainException(ErrorKeys.AuthWeakPassword, "password"),
                _ => new DomainException(ErrorKeys.AuthProviderError),
            };
        }

        var user = await response.Content.ReadFromJsonAsync<GoTrueAdminUser>(JsonOptions, cancellationToken);
        return user?.Id ?? throw new DomainException(ErrorKeys.InternalError);
    }

    public Task SetPasswordAsync(Guid userId, string newPassword, CancellationToken cancellationToken)
        => UpdateUserAsync(userId, new { password = newPassword }, cancellationToken);

    public Task BanUserAsync(Guid userId, CancellationToken cancellationToken)
        => UpdateUserAsync(userId, new { ban_duration = "876000h" }, cancellationToken);

    public Task UnbanUserAsync(Guid userId, CancellationToken cancellationToken)
        => UpdateUserAsync(userId, new { ban_duration = "none" }, cancellationToken);

    private async Task UpdateUserAsync(Guid userId, object body, CancellationToken cancellationToken)
    {
        var response = await httpClient.PutAsJsonAsync($"auth/v1/admin/users/{userId}", body, JsonOptions, cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            throw new DomainException(ErrorKeys.AuthUserNotFound);
        }

        if (!response.IsSuccessStatusCode)
        {
            throw new DomainException(ErrorKeys.AuthProviderError);
        }
    }

    private sealed record GoTrueError([property: JsonPropertyName("error_code")] string? ErrorCode);

    private sealed record GoTrueAdminUser([property: JsonPropertyName("id")] Guid Id);
}
