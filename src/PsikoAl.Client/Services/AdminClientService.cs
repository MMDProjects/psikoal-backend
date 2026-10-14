using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using PsikoAl.Common.Constants;
using PsikoAl.Common.Dtos.Admin;
using PsikoAl.Common.Dtos.Category;
using PsikoAl.Common.Dtos.Category.Create;
using PsikoAl.Common.Dtos.Category.Update;

namespace PsikoAl.Client.Services;

public sealed class AdminClientService(
    HttpClient httpClient,
    AdminSessionState session,
    IAdminAuthService auth) : IAdminClientService
{
    private static readonly object EmptyBody = new { };

    public Task<AdminApiResult<AdminDashboardStatsDto>> GetDashboardStatsAsync(CancellationToken cancellationToken)
        => GetAsync<AdminDashboardStatsDto>("admin/dashboard/stats", cancellationToken);

    public Task<AdminApiResult<IReadOnlyList<AdminUserListItemDto>>> ListUsersAsync(
        string? search,
        string? role,
        string? status,
        CancellationToken cancellationToken)
        => GetAsync<IReadOnlyList<AdminUserListItemDto>>(
            WithQuery("admin/users", ("search", search), ("role", role), ("status", status)),
            cancellationToken);

    public Task<AdminApiResult> FreezeUserAsync(Guid userId, string? reason, CancellationToken cancellationToken)
        => SendAsync(HttpMethod.Post, $"admin/users/{userId}/freeze", new AdminActionReasonDto(reason), cancellationToken);

    public Task<AdminApiResult> UnfreezeUserAsync(Guid userId, string? reason, CancellationToken cancellationToken)
        => SendAsync(HttpMethod.Post, $"admin/users/{userId}/unfreeze", new AdminActionReasonDto(reason), cancellationToken);

    public Task<AdminApiResult<IReadOnlyList<AdminExpertListItemDto>>> ListExpertsAsync(string? status, CancellationToken cancellationToken)
        => GetAsync<IReadOnlyList<AdminExpertListItemDto>>(WithQuery("admin/experts", ("status", status)), cancellationToken);

    public Task<AdminApiResult<AdminExpertDetailDto>> GetExpertDetailAsync(Guid expertId, CancellationToken cancellationToken)
        => GetAsync<AdminExpertDetailDto>($"admin/experts/{expertId}", cancellationToken);

    public Task<AdminApiResult> ApproveExpertAsync(Guid expertId, CancellationToken cancellationToken)
        => SendAsync(HttpMethod.Post, $"admin/experts/{expertId}/approve", EmptyBody, cancellationToken);

    public Task<AdminApiResult> RejectExpertAsync(Guid expertId, string reason, CancellationToken cancellationToken)
        => SendAsync(HttpMethod.Post, $"admin/experts/{expertId}/reject", new AdminActionReasonDto(reason), cancellationToken);

    public Task<AdminApiResult> SetExpertVerifiedAsync(Guid expertId, bool isVerified, CancellationToken cancellationToken)
        => SendAsync(HttpMethod.Post, $"admin/experts/{expertId}/verify", new AdminSetVerifiedDto(isVerified), cancellationToken);

    public Task<AdminApiResult<AdminExpertDocumentUrls>> GetExpertDocumentUrlsAsync(Guid expertId, CancellationToken cancellationToken)
        => GetAsync<AdminExpertDocumentUrls>($"admin/experts/{expertId}/documents", cancellationToken);

    public Task<AdminApiResult<IReadOnlyList<AdminCategoryListItemDto>>> ListCategoriesAsync(CancellationToken cancellationToken)
        => GetAsync<IReadOnlyList<AdminCategoryListItemDto>>("admin/categories", cancellationToken);

    public Task<AdminApiResult<CategoryDto>> CreateCategoryAsync(CreateCategoryDto request, CancellationToken cancellationToken)
        => SendForResultAsync<CategoryDto>(HttpMethod.Post, "admin/categories", request, cancellationToken);

    public Task<AdminApiResult<CategoryDto>> UpdateCategoryAsync(Guid categoryId, UpdateCategoryDto request, CancellationToken cancellationToken)
        => SendForResultAsync<CategoryDto>(HttpMethod.Patch, $"admin/categories/{categoryId}", request, cancellationToken);

    public Task<AdminApiResult<IReadOnlyList<AdminReviewListItemDto>>> ListReviewsAsync(string? status, CancellationToken cancellationToken)
        => GetAsync<IReadOnlyList<AdminReviewListItemDto>>(WithQuery("admin/reviews", ("status", status)), cancellationToken);

    public Task<AdminApiResult> ApproveReviewAsync(Guid reviewId, CancellationToken cancellationToken)
        => SendAsync(HttpMethod.Post, $"admin/reviews/{reviewId}/approve", EmptyBody, cancellationToken);

    public Task<AdminApiResult> RejectReviewAsync(Guid reviewId, string reason, CancellationToken cancellationToken)
        => SendAsync(HttpMethod.Post, $"admin/reviews/{reviewId}/reject", new AdminActionReasonDto(reason), cancellationToken);

    public Task<AdminApiResult<IReadOnlyList<AdminListingListItemDto>>> ListListingsAsync(string? status, CancellationToken cancellationToken)
        => GetAsync<IReadOnlyList<AdminListingListItemDto>>(WithQuery("admin/listings", ("status", status)), cancellationToken);

    public Task<AdminApiResult<AdminListingDetailDto>> GetListingDetailAsync(Guid listingId, CancellationToken cancellationToken)
        => GetAsync<AdminListingDetailDto>($"admin/listings/{listingId}", cancellationToken);

    public Task<AdminApiResult> ApproveListingAsync(Guid listingId, CancellationToken cancellationToken)
        => SendAsync(HttpMethod.Post, $"admin/listings/{listingId}/approve", EmptyBody, cancellationToken);

    public Task<AdminApiResult> RejectListingAsync(Guid listingId, string reason, CancellationToken cancellationToken)
        => SendAsync(HttpMethod.Post, $"admin/listings/{listingId}/reject", new AdminActionReasonDto(reason), cancellationToken);

    public Task<AdminApiResult> CloseListingAsync(Guid listingId, CancellationToken cancellationToken)
        => SendAsync(HttpMethod.Post, $"admin/listings/{listingId}/close", EmptyBody, cancellationToken);

    public Task<AdminApiResult> ExtendListingAsync(Guid listingId, int additionalDays, CancellationToken cancellationToken)
        => SendAsync(HttpMethod.Post, $"admin/listings/{listingId}/extend", new AdminExtendListingDto(additionalDays), cancellationToken);

    public Task<AdminApiResult> ReopenListingAsync(Guid listingId, string reason, CancellationToken cancellationToken)
        => SendAsync(HttpMethod.Post, $"admin/listings/{listingId}/reopen", new AdminActionReasonDto(reason), cancellationToken);

    public Task<AdminApiResult<IReadOnlyList<SystemSettingDto>>> ListSystemSettingsAsync(CancellationToken cancellationToken)
        => GetAsync<IReadOnlyList<SystemSettingDto>>("admin/system-settings", cancellationToken);

    public Task<AdminApiResult> UpdateSystemSettingAsync(string key, string value, CancellationToken cancellationToken)
        => SendAsync(HttpMethod.Patch, $"admin/system-settings/{Uri.EscapeDataString(key)}", new UpdateSystemSettingDto(value), cancellationToken);

    public Task<AdminApiResult<IReadOnlyList<AdminMatchListItemDto>>> ListMatchesAsync(string? status, CancellationToken cancellationToken)
        => GetAsync<IReadOnlyList<AdminMatchListItemDto>>(WithQuery("admin/matches", ("status", status)), cancellationToken);

    public Task<AdminApiResult<AdminMatchDetailDto>> GetMatchDetailAsync(Guid matchId, CancellationToken cancellationToken)
        => GetAsync<AdminMatchDetailDto>($"admin/matches/{matchId}", cancellationToken);

    public Task<AdminApiResult> ForceReleaseMatchAsync(Guid matchId, string targetStatus, string reason, CancellationToken cancellationToken)
        => SendAsync(
            HttpMethod.Post,
            $"admin/matches/{matchId}/force-release",
            new AdminForceReleaseMatchDto(targetStatus, reason),
            cancellationToken);

    public Task<AdminApiResult<IReadOnlyList<AdminNotificationTemplateDto>>> ListNotificationTemplatesAsync(CancellationToken cancellationToken)
        => GetAsync<IReadOnlyList<AdminNotificationTemplateDto>>("admin/notification-templates", cancellationToken);

    public Task<AdminApiResult<AdminNotificationTemplateDto>> UpdateNotificationTemplateAsync(
        string type,
        UpdateAdminNotificationTemplateDto request,
        CancellationToken cancellationToken)
        => SendForResultAsync<AdminNotificationTemplateDto>(
            HttpMethod.Patch,
            $"admin/notification-templates/{Uri.EscapeDataString(type)}",
            request,
            cancellationToken);

    public async Task<AdminApiResult<int>> SendNotificationAsync(AdminSendNotificationDto request, CancellationToken cancellationToken)
    {
        var result = await SendForResultAsync<AdminSendNotificationResult>(
            HttpMethod.Post,
            "admin/notifications/send",
            request,
            cancellationToken);

        return result.Value is { } sent
            ? AdminApiResult.Ok<int>(sent.RecipientCount)
            : AdminApiResult.Failure<int>(result.ErrorMessage ?? ErrorMessagesTr.For(ErrorKeys.InternalError));
    }

    public Task<AdminApiResult<IReadOnlyList<AdminAssessmentListItemDto>>> ListAssessmentsAsync(CancellationToken cancellationToken)
        => GetAsync<IReadOnlyList<AdminAssessmentListItemDto>>("admin/assessments", cancellationToken);

    public Task<AdminApiResult<AdminAssessmentDetailDto>> GetAssessmentDetailAsync(Guid assessmentId, CancellationToken cancellationToken)
        => GetAsync<AdminAssessmentDetailDto>($"admin/assessments/{assessmentId}", cancellationToken);

    public Task<AdminApiResult> UpdateAssessmentAsync(Guid assessmentId, UpdateAdminAssessmentDto request, CancellationToken cancellationToken)
        => SendAsync(HttpMethod.Patch, $"admin/assessments/{assessmentId}", request, cancellationToken);

    public Task<AdminApiResult> UpdateScoreRuleAsync(Guid scoreRuleId, UpdateAdminScoreRuleDto request, CancellationToken cancellationToken)
        => SendAsync(HttpMethod.Patch, $"admin/assessments/score-rules/{scoreRuleId}", request, cancellationToken);

    private static string WithQuery(string path, params (string Name, string? Value)[] parameters)
    {
        var query = parameters
            .Where(parameter => !string.IsNullOrWhiteSpace(parameter.Value))
            .Select(parameter => $"{parameter.Name}={Uri.EscapeDataString(parameter.Value ?? string.Empty)}")
            .ToList();

        return query.Count == 0 ? path : $"{path}?{string.Join('&', query)}";
    }

    private Task<AdminApiResult<T>> GetAsync<T>(string url, CancellationToken cancellationToken)
        => SendForResultAsync<T>(HttpMethod.Get, url, body: null, cancellationToken);

    private async Task<AdminApiResult<T>> SendForResultAsync<T>(
        HttpMethod method,
        string url,
        object? body,
        CancellationToken cancellationToken)
    {
        try
        {
            using var response = await SendAuthorizedAsync(method, url, body, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return AdminApiResult.Failure<T>(await AdminErrorMessages.FromResponseAsync(response, cancellationToken));
            }

            var value = await response.Content.ReadFromJsonAsync<T>(cancellationToken);
            return value is null
                ? AdminApiResult.Failure<T>(ErrorMessagesTr.For(ErrorKeys.InternalError))
                : AdminApiResult.Ok<T>(value);
        }
        catch (HttpRequestException)
        {
            return AdminApiResult.Failure<T>(AdminErrorMessages.ApiUnreachable);
        }
    }

    private async Task<AdminApiResult> SendAsync(HttpMethod method, string url, object? body, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await SendAuthorizedAsync(method, url, body, cancellationToken);
            return response.IsSuccessStatusCode
                ? AdminApiResult.Success
                : AdminApiResult.Failure(await AdminErrorMessages.FromResponseAsync(response, cancellationToken));
        }
        catch (HttpRequestException)
        {
            return AdminApiResult.Failure(AdminErrorMessages.ApiUnreachable);
        }
    }

    private async Task<HttpResponseMessage> SendAuthorizedAsync(
        HttpMethod method,
        string url,
        object? body,
        CancellationToken cancellationToken)
    {
        var accessToken = await auth.GetValidAccessTokenAsync(cancellationToken);
        if (accessToken is null)
        {
            return new HttpResponseMessage(HttpStatusCode.Unauthorized);
        }

        var response = await SendOnceAsync(method, url, body, accessToken, cancellationToken);
        if (response.StatusCode != HttpStatusCode.Unauthorized)
        {
            return response;
        }

        response.Dispose();
        if (!await auth.RefreshAsync(accessToken, cancellationToken) || session.AccessToken is not { } refreshedToken)
        {
            return new HttpResponseMessage(HttpStatusCode.Unauthorized);
        }

        return await SendOnceAsync(method, url, body, refreshedToken, cancellationToken);
    }

    private async Task<HttpResponseMessage> SendOnceAsync(
        HttpMethod method,
        string url,
        object? body,
        string accessToken,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(method, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        if (body is not null)
        {
            request.Content = JsonContent.Create(body, body.GetType());
        }

        return await httpClient.SendAsync(request, cancellationToken);
    }

    private sealed record AdminSendNotificationResult(bool Success, int RecipientCount);
}
