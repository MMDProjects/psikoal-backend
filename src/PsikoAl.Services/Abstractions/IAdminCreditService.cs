using PsikoAl.Common.Dtos.Admin;

namespace PsikoAl.Services.Abstractions;

public interface IAdminCreditService
{
    Task<IReadOnlyList<AdminCreditPackageDto>> ListPackagesAsync(CancellationToken cancellationToken);

    Task<AdminCreditPackageDto> CreatePackageAsync(
        Guid actorAuthUserId,
        CreateAdminCreditPackageDto request,
        CancellationToken cancellationToken);

    Task<AdminCreditPackageDto> UpdatePackageAsync(
        Guid actorAuthUserId,
        Guid packageId,
        UpdateAdminCreditPackageDto request,
        CancellationToken cancellationToken);

    Task DeletePackageAsync(Guid actorAuthUserId, Guid packageId, CancellationToken cancellationToken);

    Task<AdminPagedResultDto<AdminCreditPaymentListItemDto>> ListPaymentsAsync(
        AdminCreditPaymentQueryDto query,
        CancellationToken cancellationToken);

    Task<AdminExpertCreditsDto> GetExpertCreditsAsync(
        Guid expertId,
        int? page,
        int? perPage,
        CancellationToken cancellationToken);

    Task<AdminExpertCreditsDto> AdjustAsync(
        Guid actorAuthUserId,
        Guid expertId,
        AdminCreditAdjustDto request,
        CancellationToken cancellationToken);
}
