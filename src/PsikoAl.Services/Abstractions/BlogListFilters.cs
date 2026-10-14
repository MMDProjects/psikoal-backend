namespace PsikoAl.Services.Abstractions;

public sealed record BlogListFilters(string? Category, int? Page, int? PerPage, int? Limit)
{
    public const int DefaultPerPage = 20;

    public const int MaxPerPage = 50;

    public int EffectivePage => Limit.HasValue ? 1 : Math.Max(1, Page ?? 1);

    public int EffectivePerPage => Math.Clamp(Limit ?? PerPage ?? DefaultPerPage, 1, MaxPerPage);
}
