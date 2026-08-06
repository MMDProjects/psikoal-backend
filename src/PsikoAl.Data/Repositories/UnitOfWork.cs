using PsikoAl.Data.Repositories.Abstractions;

namespace PsikoAl.Data.Repositories;

public sealed class UnitOfWork(AppDbContext dbContext) : IUnitOfWork
{
    private IProfileRepository? _profiles;
    private IExpertRepository? _experts;
    private ICategoryRepository? _categories;
    private IReviewRepository? _reviews;
    private IAdminUserRepository? _adminUsers;
    private IAuditLogRepository? _auditLogs;

    public IProfileRepository Profiles => _profiles ??= new ProfileRepository(dbContext);

    public IExpertRepository Experts => _experts ??= new ExpertRepository(dbContext);

    public ICategoryRepository Categories => _categories ??= new CategoryRepository(dbContext);

    public IReviewRepository Reviews => _reviews ??= new ReviewRepository(dbContext);

    public IAdminUserRepository AdminUsers => _adminUsers ??= new AdminUserRepository(dbContext);

    public IAuditLogRepository AuditLogs => _auditLogs ??= new AuditLogRepository(dbContext);

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken)
        => dbContext.SaveChangesAsync(cancellationToken);
}
