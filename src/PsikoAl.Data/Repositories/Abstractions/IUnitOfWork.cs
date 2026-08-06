namespace PsikoAl.Data.Repositories.Abstractions;

public interface IUnitOfWork
{
    IProfileRepository Profiles { get; }

    IExpertRepository Experts { get; }

    ICategoryRepository Categories { get; }

    IReviewRepository Reviews { get; }

    IListingRepository Listings { get; }

    IOfferRepository Offers { get; }

    IMatchRepository Matches { get; }

    ISystemSettingRepository SystemSettings { get; }

    IAdminUserRepository AdminUsers { get; }

    IAuditLogRepository AuditLogs { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
