using Microsoft.EntityFrameworkCore;
using PsikoAl.Common.Dtos.Admin;

namespace PsikoAl.Services;

public static class AdminPaging
{
    public const int DefaultPerPage = 25;
    public const int MaxPerPage = 100;

    public static AdminPageMetaDto Normalize(int? page, int? perPage, int total = 0)
        => new(
            Math.Max(page ?? 1, 1),
            Math.Clamp(perPage ?? DefaultPerPage, 1, MaxPerPage),
            total);

    public static async Task<(List<T> Rows, AdminPageMetaDto Meta)> ToPageAsync<T>(
        this IQueryable<T> orderedQuery,
        int? page,
        int? perPage,
        CancellationToken cancellationToken)
    {
        var requested = Normalize(page, perPage);
        var total = await orderedQuery.CountAsync(cancellationToken);
        var rows = await orderedQuery
            .Skip((requested.Page - 1) * requested.PerPage)
            .Take(requested.PerPage)
            .ToListAsync(cancellationToken);

        return (rows, requested with { Total = total });
    }
}
