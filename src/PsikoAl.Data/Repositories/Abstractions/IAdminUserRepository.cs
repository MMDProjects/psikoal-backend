using PsikoAl.Data.Entities;

namespace PsikoAl.Data.Repositories.Abstractions;

public interface IAdminUserRepository : IRepository<AdminUser, Guid>
{
    Task<AdminUser?> GetActiveByAuthUserIdAsync(Guid authUserId, CancellationToken cancellationToken);
}
