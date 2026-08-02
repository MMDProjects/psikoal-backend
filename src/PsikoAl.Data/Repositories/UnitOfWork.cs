using PsikoAl.Data.Repositories.Abstractions;

namespace PsikoAl.Data.Repositories;

public sealed class UnitOfWork(AppDbContext dbContext) : IUnitOfWork
{
    private IProfileRepository? _profiles;
    private IAdminUserRepository? _adminUsers;
    private IAuditLogRepository? _auditLogs;

    public IProfileRepository Profiles => _profiles ??= new ProfileRepository(dbContext);

    public IAdminUserRepository AdminUsers => _adminUsers ??= new AdminUserRepository(dbContext);

    public IAuditLogRepository AuditLogs => _auditLogs ??= new AuditLogRepository(dbContext);

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken)
        => dbContext.SaveChangesAsync(cancellationToken);
}
