using PsikoAl.Common.Dtos.Admin;
using PsikoAl.Data.Entities;

namespace PsikoAl.Services.Abstractions;

public interface IAdminGuard
{
    Task<AdminUser?> GetActiveAdminAsync(Guid authUserId, CancellationToken cancellationToken);

    Task<AdminMeDto> GetCurrentAsync(Guid authUserId, CancellationToken cancellationToken);
}
