using Microsoft.EntityFrameworkCore;
using PsikoAl.Common.Dtos.Admin;
using PsikoAl.Data.Repositories.Abstractions;
using PsikoAl.Services.Abstractions;

namespace PsikoAl.Services;

public sealed class AdminAuditLogService(IUnitOfWork unitOfWork) : IAdminAuditLogService
{
    public async Task<AdminPagedResultDto<AdminAuditLogDto>> ListAsync(
        AdminAuditLogQueryDto query,
        CancellationToken cancellationToken)
    {
        var logs =
            from log in unitOfWork.AuditLogs.Query()
            join admin in unitOfWork.AdminUsers.Query() on log.AdminUserId equals (Guid?)admin.Id into admins
            from admin in admins.DefaultIfEmpty()
            select new { log, AdminDisplayName = admin == null ? null : admin.DisplayName };

        if (!string.IsNullOrWhiteSpace(query.Actor))
        {
            var pattern = $"%{query.Actor.Trim()}%";
            logs = logs.Where(row => row.AdminDisplayName != null && EF.Functions.ILike(row.AdminDisplayName, pattern));
        }

        if (!string.IsNullOrWhiteSpace(query.Action))
        {
            logs = logs.Where(row => row.log.Action == query.Action);
        }

        if (!string.IsNullOrWhiteSpace(query.EntityType))
        {
            logs = logs.Where(row => row.log.EntityType == query.EntityType);
        }

        if (!string.IsNullOrWhiteSpace(query.EntityId))
        {
            logs = logs.Where(row => row.log.EntityId == query.EntityId);
        }

        if (query.From is { } from)
        {
            var fromUtc = StartOfDayUtc(from);
            logs = logs.Where(row => row.log.CreatedAt >= fromUtc);
        }

        if (query.To is { } to)
        {
            var toExclusiveUtc = StartOfDayUtc(to.AddDays(1));
            logs = logs.Where(row => row.log.CreatedAt < toExclusiveUtc);
        }

        var (rows, meta) = await logs
            .OrderByDescending(row => row.log.CreatedAt)
            .ThenByDescending(row => row.log.Id)
            .Select(row => new AdminAuditLogDto(
                row.log.Id,
                row.log.ActorType,
                row.log.AdminUserId,
                row.AdminDisplayName,
                row.log.Action,
                row.log.EntityType,
                row.log.EntityId,
                row.log.OldValue,
                row.log.NewValue,
                row.log.Reason,
                row.log.CreatedAt))
            .ToPageAsync(query.Page, query.PerPage, cancellationToken);

        return new AdminPagedResultDto<AdminAuditLogDto>(rows, meta);
    }

    private static DateTimeOffset StartOfDayUtc(DateOnly date)
        => new(date.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
}
