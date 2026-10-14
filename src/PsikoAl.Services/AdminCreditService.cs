using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PsikoAl.Common.Constants;
using PsikoAl.Common.Dtos.Admin;
using PsikoAl.Common.Exceptions;
using PsikoAl.Common.Presentation;
using PsikoAl.Data.Entities;
using PsikoAl.Data.Repositories.Abstractions;
using PsikoAl.Services.Abstractions;

namespace PsikoAl.Services;

// Finans paneli (AdminPolicies.Finance): paketler, satın almalar, uzman defteri ve elle
// düzeltme. Her yazma işlemi audit log'a düşer; elle düzeltme bakiyeyi eksiye düşüremez.
public sealed class AdminCreditService(IUnitOfWork unitOfWork, IAdminGuard adminGuard) : IAdminCreditService
{
    private const string PackageEntityType = "credit_package";
    private const string ExpertEntityType = "expert_credit";

    public async Task<IReadOnlyList<AdminCreditPackageDto>> ListPackagesAsync(CancellationToken cancellationToken)
    {
        var packages = await unitOfWork.CreditPackages.Query()
            .OrderBy(package => package.SortOrder)
            .ThenBy(package => package.Credits)
            .ToListAsync(cancellationToken);

        var paidCounts = await unitOfWork.CreditPayments.Query()
            .Where(payment => payment.Status == CreditPaymentStatuses.Paid)
            .GroupBy(payment => payment.PackageId)
            .Select(group => new { PackageId = group.Key, Count = group.Count() })
            .ToDictionaryAsync(row => row.PackageId, row => row.Count, cancellationToken);

        return packages.Select(package => ToPackageDto(package, paidCounts.GetValueOrDefault(package.Id))).ToList();
    }

    public async Task<AdminCreditPackageDto> CreatePackageAsync(
        Guid actorAuthUserId,
        CreateAdminCreditPackageDto request,
        CancellationToken cancellationToken)
    {
        var actor = await GetRequiredActorAsync(actorAuthUserId, cancellationToken);
        var package = new CreditPackage
        {
            Name = request.Name.Trim(),
            Credits = request.Credits,
            PriceTry = request.PriceTry,
            IsActive = request.IsActive,
            SortOrder = request.SortOrder,
        };

        await unitOfWork.CreditPackages.AddAsync(package, cancellationToken);
        await AddAuditAsync(actor.Id, AuditActions.CreditPackageCreate, PackageEntityType, package.Id.ToString(), null, Snapshot(package), null, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ToPackageDto(package, paidPurchaseCount: 0);
    }

    public async Task<AdminCreditPackageDto> UpdatePackageAsync(
        Guid actorAuthUserId,
        Guid packageId,
        UpdateAdminCreditPackageDto request,
        CancellationToken cancellationToken)
    {
        var actor = await GetRequiredActorAsync(actorAuthUserId, cancellationToken);
        var package = await GetRequiredPackageAsync(packageId, cancellationToken);
        var oldValue = Snapshot(package);

        package.Name = request.Name.Trim();
        package.Credits = request.Credits;
        package.PriceTry = request.PriceTry;
        package.IsActive = request.IsActive;
        package.SortOrder = request.SortOrder;

        await AddAuditAsync(actor.Id, AuditActions.CreditPackageUpdate, PackageEntityType, package.Id.ToString(), oldValue, Snapshot(package), null, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var paidPurchaseCount = await unitOfWork.CreditPayments.Query()
            .CountAsync(payment => payment.PackageId == package.Id && payment.Status == CreditPaymentStatuses.Paid, cancellationToken);
        return ToPackageDto(package, paidPurchaseCount);
    }

    // Satın alma geçmişi olan paket silinmez (credit_payments FK'si RESTRICT); pasife alınır.
    public async Task DeletePackageAsync(Guid actorAuthUserId, Guid packageId, CancellationToken cancellationToken)
    {
        var actor = await GetRequiredActorAsync(actorAuthUserId, cancellationToken);
        var package = await GetRequiredPackageAsync(packageId, cancellationToken);

        if (await unitOfWork.CreditPayments.AnyForPackageAsync(package.Id, cancellationToken))
        {
            throw new DomainException(ErrorKeys.CreditPackageInUse);
        }

        await AddAuditAsync(actor.Id, AuditActions.CreditPackageDelete, PackageEntityType, package.Id.ToString(), Snapshot(package), null, null, cancellationToken);
        unitOfWork.CreditPackages.Delete(package);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<AdminPagedResultDto<AdminCreditPaymentListItemDto>> ListPaymentsAsync(
        AdminCreditPaymentQueryDto query,
        CancellationToken cancellationToken)
    {
        var payments = unitOfWork.CreditPayments.Query();
        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            payments = payments.Where(payment => payment.Status == query.Status);
        }

        var (rows, meta) = await payments
            .OrderByDescending(payment => payment.CreatedAt)
            .Select(payment => new AdminCreditPaymentListItemDto(
                payment.Id,
                payment.MerchantOid,
                payment.ExpertId,
                payment.Expert != null && payment.Expert.Profile != null
                    ? payment.Expert.Profile.FirstName + " " + payment.Expert.Profile.LastName
                    : string.Empty,
                payment.Expert != null && payment.Expert.Profile != null ? payment.Expert.Profile.Email : string.Empty,
                payment.Package != null ? payment.Package.Name : string.Empty,
                payment.Credits,
                payment.AmountTry,
                payment.Status,
                payment.CreatedAt,
                payment.CompletedAt))
            .ToPageAsync(query.Page, query.PerPage, cancellationToken);

        return new AdminPagedResultDto<AdminCreditPaymentListItemDto>(rows, meta);
    }

    public async Task<AdminExpertCreditsDto> GetExpertCreditsAsync(
        Guid expertId,
        int? page,
        int? perPage,
        CancellationToken cancellationToken)
    {
        var expert = await unitOfWork.Experts.GetWithProfileAsync(expertId, cancellationToken)
            ?? throw new DomainException(ErrorKeys.ExpertNotFound);
        var balance = await unitOfWork.ExpertCredits.GetBalanceAsync(expertId, cancellationToken)
            ?? throw new DomainException(ErrorKeys.ExpertNotFound);

        var (rows, meta) = await unitOfWork.CreditTransactions.QueryForExpert(expertId)
            .Select(transaction => new
            {
                transaction.Id,
                transaction.Amount,
                transaction.BalanceAfter,
                transaction.Type,
                transaction.Reason,
                transaction.OfferId,
                transaction.PaymentId,
                AdminName = transaction.AdminUser != null ? transaction.AdminUser.DisplayName : null,
                transaction.CreatedAt,
            })
            .ToPageAsync(page, perPage, cancellationToken);

        var transactions = rows
            .Select(row => new AdminCreditTransactionDto(
                row.Id,
                row.Amount,
                row.BalanceAfter,
                row.Type,
                CreditLabelFormatter.TypeLabel(row.Type),
                row.Reason,
                row.OfferId,
                row.PaymentId,
                row.AdminName,
                row.CreatedAt))
            .ToList();

        var fullName = expert.Profile is { } profile ? $"{profile.FirstName} {profile.LastName}".Trim() : string.Empty;
        return new AdminExpertCreditsDto(
            expert.Id,
            fullName,
            balance,
            new AdminPagedResultDto<AdminCreditTransactionDto>(transactions, meta));
    }

    public async Task<AdminExpertCreditsDto> AdjustAsync(
        Guid actorAuthUserId,
        Guid expertId,
        AdminCreditAdjustDto request,
        CancellationToken cancellationToken)
    {
        var actor = await GetRequiredActorAsync(actorAuthUserId, cancellationToken);
        var reason = request.Reason?.Trim();
        if (string.IsNullOrEmpty(reason))
        {
            throw new DomainException(ErrorKeys.CreditAdjustReasonRequired, "reason");
        }

        if (request.Amount == 0 || Math.Abs(request.Amount) > CreditFieldLimits.AdjustMaxAbsoluteAmount)
        {
            throw new DomainException(ErrorKeys.ValidationFailed, "amount");
        }

        if (!await unitOfWork.Experts.ExistsAsync(expertId, cancellationToken))
        {
            throw new DomainException(ErrorKeys.ExpertNotFound);
        }

        await using (var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken))
        {
            var balanceAfter = await unitOfWork.ExpertCredits.TryApplyDeltaAsync(expertId, request.Amount, cancellationToken)
                ?? throw new DomainException(ErrorKeys.CreditBalanceNegative, "amount");

            await unitOfWork.CreditTransactions.AddAsync(
                new CreditTransaction
                {
                    ExpertId = expertId,
                    Amount = request.Amount,
                    BalanceAfter = balanceAfter,
                    Type = CreditTransactionTypes.AdminAdjust,
                    AdminUserId = actor.Id,
                    Reason = reason,
                },
                cancellationToken);

            await AddAuditAsync(
                actor.Id,
                AuditActions.CreditAdjust,
                ExpertEntityType,
                expertId.ToString(),
                JsonSerializer.Serialize(new { balance = balanceAfter - request.Amount }),
                JsonSerializer.Serialize(new { balance = balanceAfter, amount = request.Amount }),
                reason,
                cancellationToken);

            await unitOfWork.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }

        return await GetExpertCreditsAsync(expertId, page: 1, perPage: null, cancellationToken);
    }

    private static AdminCreditPackageDto ToPackageDto(CreditPackage package, int paidPurchaseCount)
        => new(
            package.Id,
            package.Name,
            package.Credits,
            package.PriceTry,
            package.IsActive,
            package.SortOrder,
            paidPurchaseCount,
            package.UpdatedAt);

    private static string Snapshot(CreditPackage package)
        => JsonSerializer.Serialize(new
        {
            package.Name,
            package.Credits,
            package.PriceTry,
            package.IsActive,
            package.SortOrder,
        });

    private async Task<CreditPackage> GetRequiredPackageAsync(Guid packageId, CancellationToken cancellationToken)
        => await unitOfWork.CreditPackages.GetByIdAsync(packageId, cancellationToken)
            ?? throw new DomainException(ErrorKeys.CreditPackageNotFound);

    private async Task<AdminUser> GetRequiredActorAsync(Guid actorAuthUserId, CancellationToken cancellationToken)
        => await adminGuard.GetActiveAdminAsync(actorAuthUserId, cancellationToken)
            ?? throw new DomainException(ErrorKeys.AdminUserNotFound);

    private Task AddAuditAsync(
        Guid adminUserId,
        string action,
        string entityType,
        string entityId,
        string? oldValue,
        string? newValue,
        string? reason,
        CancellationToken cancellationToken)
        => unitOfWork.AuditLogs.AddAsync(
            new AuditLog
            {
                AdminUserId = adminUserId,
                ActorType = AuditActorTypes.Admin,
                Action = action,
                EntityType = entityType,
                EntityId = entityId,
                OldValue = oldValue,
                NewValue = newValue,
                Reason = reason,
            },
            cancellationToken);
}
