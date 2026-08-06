using PsikoAl.Common.Dtos.Admin;
using PsikoAl.Common.Dtos.Category;
using PsikoAl.Common.Dtos.Category.Create;
using PsikoAl.Common.Dtos.Category.Update;

namespace PsikoAl.Client.Services;

public interface IAdminClientService
{
    Task<bool> LoginAsync(string email, string password, CancellationToken cancellationToken);

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
}

public sealed record AdminExpertDocumentUrls(string? CvUrl, IReadOnlyList<string> CertificateUrls);
