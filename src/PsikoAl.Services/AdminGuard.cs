using PsikoAl.Data.Entities;
using PsikoAl.Data.Repositories.Abstractions;
using PsikoAl.Services.Abstractions;

namespace PsikoAl.Services;

public sealed class AdminGuard(IUnitOfWork unitOfWork) : IAdminGuard
{
    public Task<AdminUser?> GetActiveAdminAsync(Guid authUserId, CancellationToken cancellationToken)
        => unitOfWork.AdminUsers.GetActiveByAuthUserIdAsync(authUserId, cancellationToken);
}
