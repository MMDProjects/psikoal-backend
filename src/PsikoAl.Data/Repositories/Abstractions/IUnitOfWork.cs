namespace PsikoAl.Data.Repositories.Abstractions;

public interface IUnitOfWork
{
    IProfileRepository Profiles { get; }

    IAdminUserRepository AdminUsers { get; }

    IAuditLogRepository AuditLogs { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
