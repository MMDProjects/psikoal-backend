using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using PsikoAl.Data;

namespace PsikoAl.Api.Extensions;

// AddDbContextCheck hata sebebini yutuyordu: log yalnız "Unhealthy ... message '(null)'"
// diyordu ve canlıda bağlantı dizesi mi, şifre mi, ağ mı bilinemiyordu. Bu kontrol aynı
// işi yapar (bağlantı aç + SELECT 1) ve istisnanın türünü ve mesajını açıklamaya yazar.
// Npgsql mesajları parola içermez; bağlantı dizesi açıklamaya hiç yazılmaz.
public sealed class DatabaseHealthCheck(AppDbContext dbContext) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await dbContext.Database.ExecuteSqlRawAsync("select 1", cancellationToken);
            return HealthCheckResult.Healthy();
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            var root = exception.GetBaseException();
            return HealthCheckResult.Unhealthy($"{root.GetType().Name}: {root.Message}", exception);
        }
    }
}
