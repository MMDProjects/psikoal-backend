using Microsoft.EntityFrameworkCore;
using PsikoAl.Data.Entities;
using PsikoAl.Data.Repositories.Abstractions;

namespace PsikoAl.Data.Repositories;

public sealed class AdminUserRepository(AppDbContext dbContext)
    : Repository<AdminUser, Guid>(dbContext), IAdminUserRepository
{
    public Task<AdminUser?> GetActiveByAuthUserIdAsync(Guid authUserId, CancellationToken cancellationToken)
        => Query().FirstOrDefaultAsync(
            adminUser => adminUser.AuthUserId == authUserId && adminUser.IsActive,
            cancellationToken);
}
