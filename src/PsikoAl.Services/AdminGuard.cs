using PsikoAl.Common.Constants;
using PsikoAl.Common.Dtos.Admin;
using PsikoAl.Common.Exceptions;
using PsikoAl.Data.Entities;
using PsikoAl.Data.Repositories.Abstractions;
using PsikoAl.Services.Abstractions;

namespace PsikoAl.Services;

public sealed class AdminGuard(IUnitOfWork unitOfWork) : IAdminGuard
{
    public Task<AdminUser?> GetActiveAdminAsync(Guid authUserId, CancellationToken cancellationToken)
        => unitOfWork.AdminUsers.GetActiveByAuthUserIdAsync(authUserId, cancellationToken);

    public async Task<AdminMeDto> GetCurrentAsync(Guid authUserId, CancellationToken cancellationToken)
    {
        var admin = await GetActiveAdminAsync(authUserId, cancellationToken)
            ?? throw new DomainException(ErrorKeys.AdminUserNotFound);

        return new AdminMeDto(admin.Id, admin.DisplayName, admin.Role);
    }
}
