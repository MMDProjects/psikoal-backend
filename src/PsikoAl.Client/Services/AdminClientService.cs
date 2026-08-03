using System.Net.Http.Headers;
using System.Net.Http.Json;
using PsikoAl.Client.Services;
using PsikoAl.Common.Dtos.Admin;
using PsikoAl.Common.Dtos.Auth;

namespace PsikoAl.Client.Services;

public sealed class AdminClientService(HttpClient httpClient, AdminSessionState session) : IAdminClientService
{
    public async Task<bool> LoginAsync(string email, string password, CancellationToken cancellationToken)
    {
        var response = await httpClient.PostAsJsonAsync("auth/login", new { email, password }, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return false;
        }

        var login = await response.Content.ReadFromJsonAsync<LoginResponseDto>(cancellationToken);
        if (login is null)
        {
            return false;
        }

        session.SignIn(login.Tokens.AccessToken, login.User.Email);
        return true;
    }

    public async Task<IReadOnlyList<AdminUserListItemDto>?> ListUsersAsync(
        string? search,
        string? role,
        string? status,
        CancellationToken cancellationToken)
    {
        var query = new List<string>();
        if (!string.IsNullOrWhiteSpace(search))
        {
            query.Add($"search={Uri.EscapeDataString(search)}");
        }

        if (!string.IsNullOrWhiteSpace(role))
        {
            query.Add($"role={Uri.EscapeDataString(role)}");
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query.Add($"status={Uri.EscapeDataString(status)}");
        }

        var url = "admin/users" + (query.Count > 0 ? "?" + string.Join("&", query) : string.Empty);
        using var request = CreateAuthorizedRequest(HttpMethod.Get, url);
        var response = await httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<List<AdminUserListItemDto>>(cancellationToken);
    }

    public Task<bool> FreezeUserAsync(Guid userId, string? reason, CancellationToken cancellationToken)
        => PostUserActionAsync($"admin/users/{userId}/freeze", reason, cancellationToken);

    public Task<bool> UnfreezeUserAsync(Guid userId, string? reason, CancellationToken cancellationToken)
        => PostUserActionAsync($"admin/users/{userId}/unfreeze", reason, cancellationToken);

    private async Task<bool> PostUserActionAsync(string url, string? reason, CancellationToken cancellationToken)
    {
        using var request = CreateAuthorizedRequest(HttpMethod.Post, url);
        request.Content = JsonContent.Create(new AdminActionReasonDto(reason));
        var response = await httpClient.SendAsync(request, cancellationToken);
        return response.IsSuccessStatusCode;
    }

    public async Task<IReadOnlyList<AdminExpertListItemDto>?> ListExpertsAsync(string? status, CancellationToken cancellationToken)
    {
        var url = "admin/experts" + (string.IsNullOrWhiteSpace(status) ? string.Empty : $"?status={Uri.EscapeDataString(status)}");
        using var request = CreateAuthorizedRequest(HttpMethod.Get, url);
        var response = await httpClient.SendAsync(request, cancellationToken);
        return response.IsSuccessStatusCode
            ? await response.Content.ReadFromJsonAsync<List<AdminExpertListItemDto>>(cancellationToken)
            : null;
    }

    public async Task<AdminExpertDetailDto?> GetExpertDetailAsync(Guid expertId, CancellationToken cancellationToken)
    {
        using var request = CreateAuthorizedRequest(HttpMethod.Get, $"admin/experts/{expertId}");
        var response = await httpClient.SendAsync(request, cancellationToken);
        return response.IsSuccessStatusCode
            ? await response.Content.ReadFromJsonAsync<AdminExpertDetailDto>(cancellationToken)
            : null;
    }

    public async Task<bool> ApproveExpertAsync(Guid expertId, CancellationToken cancellationToken)
    {
        using var request = CreateAuthorizedRequest(HttpMethod.Post, $"admin/experts/{expertId}/approve");
        request.Content = JsonContent.Create(new { });
        var response = await httpClient.SendAsync(request, cancellationToken);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> RejectExpertAsync(Guid expertId, string reason, CancellationToken cancellationToken)
    {
        using var request = CreateAuthorizedRequest(HttpMethod.Post, $"admin/experts/{expertId}/reject");
        request.Content = JsonContent.Create(new AdminActionReasonDto(reason));
        var response = await httpClient.SendAsync(request, cancellationToken);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> SetExpertVerifiedAsync(Guid expertId, bool isVerified, CancellationToken cancellationToken)
    {
        using var request = CreateAuthorizedRequest(HttpMethod.Post, $"admin/experts/{expertId}/verify");
        request.Content = JsonContent.Create(new AdminSetVerifiedDto(isVerified));
        var response = await httpClient.SendAsync(request, cancellationToken);
        return response.IsSuccessStatusCode;
    }

    public async Task<AdminExpertDocumentUrls?> GetExpertDocumentUrlsAsync(Guid expertId, CancellationToken cancellationToken)
    {
        using var request = CreateAuthorizedRequest(HttpMethod.Get, $"admin/experts/{expertId}/documents");
        var response = await httpClient.SendAsync(request, cancellationToken);
        return response.IsSuccessStatusCode
            ? await response.Content.ReadFromJsonAsync<AdminExpertDocumentUrls>(cancellationToken)
            : null;
    }

    private HttpRequestMessage CreateAuthorizedRequest(HttpMethod method, string url)
    {
        var request = new HttpRequestMessage(method, url);
        if (session.AccessToken is not null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", session.AccessToken);
        }

        return request;
    }
}
