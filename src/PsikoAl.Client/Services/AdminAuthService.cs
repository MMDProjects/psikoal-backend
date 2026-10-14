using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using PsikoAl.Common.Dtos.Admin;
using PsikoAl.Common.Dtos.Auth;
using PsikoAl.Common.Dtos.Auth.Create;

namespace PsikoAl.Client.Services;

public sealed class AdminAuthService(
    HttpClient httpClient,
    AdminSessionState session,
    AdminSessionStore store,
    TimeProvider timeProvider) : IAdminAuthService
{
    private static readonly TimeSpan RefreshMargin = TimeSpan.FromMinutes(2);

    public async Task<AdminApiResult> LoginAsync(string email, string password, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await httpClient.PostAsJsonAsync(
                "auth/login",
                new LoginRequestDto(email, password),
                cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return AdminApiResult.Failure(response.StatusCode == HttpStatusCode.UnprocessableEntity
                    ? await AdminErrorMessages.FromResponseAsync(response, cancellationToken)
                    : AdminErrorMessages.LoginFailed);
            }

            var login = await response.Content.ReadFromJsonAsync<LoginResponseDto>(cancellationToken);
            if (login is null)
            {
                return AdminApiResult.Failure(AdminErrorMessages.LoginFailed);
            }

            var admin = await FetchAdminAsync(login.Tokens.AccessToken, cancellationToken);
            if (admin.Value is null)
            {
                return AdminApiResult.Failure(admin.ErrorMessage ?? AdminErrorMessages.NotAnAdmin);
            }

            var adminSession = new AdminSession(
                login.Tokens.AccessToken,
                login.Tokens.RefreshToken,
                ExpiresAt(login.Tokens.ExpiresIn),
                login.User.Email,
                admin.Value.DisplayName,
                admin.Value.Role);

            session.SignIn(adminSession);
            await store.SaveAsync(adminSession);
            return AdminApiResult.Success;
        }
        catch (HttpRequestException)
        {
            return AdminApiResult.Failure(AdminErrorMessages.ApiUnreachable);
        }
    }

    public async Task RestoreAsync(CancellationToken cancellationToken)
    {
        if (session.IsRestored)
        {
            return;
        }

        var stored = await store.LoadAsync();
        if (stored is null)
        {
            session.MarkRestored();
            return;
        }

        session.SignIn(stored);

        try
        {
            var accessToken = await GetValidAccessTokenAsync(cancellationToken);
            if (accessToken is null)
            {
                return;
            }

            var admin = await FetchAdminAsync(accessToken, cancellationToken);
            if (admin.Value is null)
            {
                await ClearSessionAsync();
                return;
            }

            var current = session.Current;
            if (current is not null && (current.Role != admin.Value.Role || current.DisplayName != admin.Value.DisplayName))
            {
                var updated = current with { Role = admin.Value.Role, DisplayName = admin.Value.DisplayName };
                session.SignIn(updated);
                await store.SaveAsync(updated);
            }
        }
        catch (HttpRequestException)
        {
            await ClearSessionAsync();
        }
    }

    public async Task<string?> GetValidAccessTokenAsync(CancellationToken cancellationToken)
    {
        var current = session.Current;
        if (current is null)
        {
            return null;
        }

        if (current.AccessTokenExpiresAt - timeProvider.GetUtcNow() > RefreshMargin)
        {
            return current.AccessToken;
        }

        return await RefreshAsync(current.AccessToken, cancellationToken) ? session.AccessToken : null;
    }

    public async Task<bool> RefreshAsync(string? staleAccessToken, CancellationToken cancellationToken)
    {
        await session.RefreshLock.WaitAsync(cancellationToken);
        try
        {
            var current = session.Current;
            if (current is null)
            {
                return false;
            }

            if (current.AccessToken != staleAccessToken)
            {
                return true;
            }

            using var response = await httpClient.PostAsJsonAsync(
                "auth/refresh",
                new RefreshRequestDto(current.RefreshToken),
                cancellationToken);
            var tokens = response.IsSuccessStatusCode
                ? await response.Content.ReadFromJsonAsync<AuthTokensDto>(cancellationToken)
                : null;
            if (tokens is null)
            {
                await ClearSessionAsync();
                return false;
            }

            var refreshed = current with
            {
                AccessToken = tokens.AccessToken,
                RefreshToken = tokens.RefreshToken,
                AccessTokenExpiresAt = ExpiresAt(tokens.ExpiresIn),
            };
            session.SignIn(refreshed);
            await store.SaveAsync(refreshed);
            return true;
        }
        catch (HttpRequestException)
        {
            return false;
        }
        finally
        {
            session.RefreshLock.Release();
        }
    }

    public async Task LogoutAsync(CancellationToken cancellationToken)
    {
        var accessToken = session.AccessToken;
        await ClearSessionAsync();
        if (accessToken is null)
        {
            return;
        }

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "auth/logout");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            using var response = await httpClient.SendAsync(request, cancellationToken);
        }
        catch (HttpRequestException)
        {
            return;
        }
    }

    private async Task<AdminApiResult<AdminMeDto>> FetchAdminAsync(string accessToken, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "admin/me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        using var response = await httpClient.SendAsync(request, cancellationToken);

        if (response.StatusCode == HttpStatusCode.Forbidden)
        {
            return AdminApiResult.Failure<AdminMeDto>(AdminErrorMessages.NotAnAdmin);
        }

        if (!response.IsSuccessStatusCode)
        {
            return AdminApiResult.Failure<AdminMeDto>(await AdminErrorMessages.FromResponseAsync(response, cancellationToken));
        }

        var admin = await response.Content.ReadFromJsonAsync<AdminMeDto>(cancellationToken);
        return admin is null
            ? AdminApiResult.Failure<AdminMeDto>(AdminErrorMessages.NotAnAdmin)
            : AdminApiResult.Ok<AdminMeDto>(admin);
    }

    private DateTimeOffset ExpiresAt(int expiresInSeconds)
        => timeProvider.GetUtcNow().AddSeconds(expiresInSeconds);

    private async Task ClearSessionAsync()
    {
        session.SignOut();
        await store.ClearAsync();
    }
}
