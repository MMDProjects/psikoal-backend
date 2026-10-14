namespace PsikoAl.Common.Dtos.Admin;

public sealed record AdminUserListQueryDto(string? Search, string? Role, string? Status, int? Page, int? PerPage);
