namespace PsikoAl.Common.Dtos.Admin;

public sealed record AdminPagedResultDto<T>(IReadOnlyList<T> Data, AdminPageMetaDto Meta);
