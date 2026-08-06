namespace PsikoAl.Data.Repositories.Abstractions;

public interface IUnitOfWork
{
    IProfileRepository Profiles { get; }

    IExpertRepository Experts { get; }

    ICategoryRepository Categories { get; }

    IReviewRepository Reviews { get; }

    IAdminUserRepository AdminUsers { get; }

    IAuditLogRepository AuditLogs { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
