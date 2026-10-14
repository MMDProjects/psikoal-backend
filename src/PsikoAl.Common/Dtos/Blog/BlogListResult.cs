namespace PsikoAl.Common.Dtos.Blog;

public sealed record BlogListResult(IReadOnlyList<BlogListItemDto> Data, int Page, int PerPage, int Total);
