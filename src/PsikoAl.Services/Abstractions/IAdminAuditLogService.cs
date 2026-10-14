using PsikoAl.Common.Dtos.Admin;

namespace PsikoAl.Services.Abstractions;

public interface IAdminAuditLogService
{
    Task<AdminPagedResultDto<AdminAuditLogDto>> ListAsync(AdminAuditLogQueryDto query, CancellationToken cancellationToken);
}
