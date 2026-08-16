using PsikoAl.Common.Dtos.Admin;
using PsikoAl.Common.Dtos.Category;
using PsikoAl.Common.Dtos.Category.Create;
using PsikoAl.Common.Dtos.Category.Update;

namespace PsikoAl.Client.Services;

public interface IAdminClientService
{
    Task<bool> LoginAsync(string email, string password, CancellationToken cancellationToken);

    Task<AdminDashboardStatsDto?> GetDashboardStatsAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<AdminUserListItemDto>?> ListUsersAsync(
        string? search,
        string? role,
        string? status,
        CancellationToken cancellationToken);

    Task<bool> FreezeUserAsync(Guid userId, string? reason, CancellationToken cancellationToken);

    Task<bool> UnfreezeUserAsync(Guid userId, string? reason, CancellationToken cancellationToken);

    Task<IReadOnlyList<AdminExpertListItemDto>?> ListExpertsAsync(string? status, CancellationToken cancellationToken);

    Task<AdminExpertDetailDto?> GetExpertDetailAsync(Guid expertId, CancellationToken cancellationToken);

    Task<bool> ApproveExpertAsync(Guid expertId, CancellationToken cancellationToken);

    Task<bool> RejectExpertAsync(Guid expertId, string reason, CancellationToken cancellationToken);

    Task<bool> SetExpertVerifiedAsync(Guid expertId, bool isVerified, CancellationToken cancellationToken);

    Task<AdminExpertDocumentUrls?> GetExpertDocumentUrlsAsync(Guid expertId, CancellationToken cancellationToken);

    Task<IReadOnlyList<AdminCategoryListItemDto>?> ListCategoriesAsync(CancellationToken cancellationToken);

    Task<CategoryDto?> CreateCategoryAsync(CreateCategoryDto request, CancellationToken cancellationToken);

    Task<CategoryDto?> UpdateCategoryAsync(Guid categoryId, UpdateCategoryDto request, CancellationToken cancellationToken);

    Task<IReadOnlyList<AdminReviewListItemDto>?> ListReviewsAsync(string? status, CancellationToken cancellationToken);

    Task<bool> ApproveReviewAsync(Guid reviewId, CancellationToken cancellationToken);

    Task<bool> RejectReviewAsync(Guid reviewId, string reason, CancellationToken cancellationToken);

    Task<IReadOnlyList<AdminListingListItemDto>?> ListListingsAsync(string? status, CancellationToken cancellationToken);

    Task<AdminListingDetailDto?> GetListingDetailAsync(Guid listingId, CancellationToken cancellationToken);

    Task<bool> ApproveListingAsync(Guid listingId, CancellationToken cancellationToken);

    Task<bool> RejectListingAsync(Guid listingId, string reason, CancellationToken cancellationToken);

    Task<bool> CloseListingAsync(Guid listingId, CancellationToken cancellationToken);

    Task<bool> ExtendListingAsync(Guid listingId, int additionalDays, CancellationToken cancellationToken);

    Task<bool> ReopenListingAsync(Guid listingId, string reason, CancellationToken cancellationToken);

    Task<IReadOnlyList<SystemSettingDto>?> ListSystemSettingsAsync(CancellationToken cancellationToken);

    Task<bool> UpdateSystemSettingAsync(string key, string value, CancellationToken cancellationToken);

    Task<IReadOnlyList<AdminMatchListItemDto>?> ListMatchesAsync(string? status, CancellationToken cancellationToken);

    Task<AdminMatchDetailDto?> GetMatchDetailAsync(Guid matchId, CancellationToken cancellationToken);

    Task<bool> ForceReleaseMatchAsync(Guid matchId, string targetStatus, string reason, CancellationToken cancellationToken);

    Task<IReadOnlyList<AdminNotificationTemplateDto>?> ListNotificationTemplatesAsync(CancellationToken cancellationToken);

    Task<AdminNotificationTemplateDto?> UpdateNotificationTemplateAsync(
        string type,
        UpdateAdminNotificationTemplateDto request,
        CancellationToken cancellationToken);

    Task<int?> SendNotificationAsync(AdminSendNotificationDto request, CancellationToken cancellationToken);

    Task<IReadOnlyList<AdminAssessmentListItemDto>?> ListAssessmentsAsync(CancellationToken cancellationToken);

    Task<AdminAssessmentDetailDto?> GetAssessmentDetailAsync(Guid assessmentId, CancellationToken cancellationToken);

    Task<bool> UpdateAssessmentAsync(Guid assessmentId, UpdateAdminAssessmentDto request, CancellationToken cancellationToken);

    Task<bool> UpdateScoreRuleAsync(Guid scoreRuleId, UpdateAdminScoreRuleDto request, CancellationToken cancellationToken);
}

public sealed record AdminExpertDocumentUrls(string? CvUrl, IReadOnlyList<string> CertificateUrls);
