using PsikoAl.Common.Dtos.Admin;

namespace PsikoAl.Services.Abstractions;

public interface IAdminDashboardService
{
    Task<AdminDashboardStatsDto> GetStatsAsync(CancellationToken cancellationToken);
}
