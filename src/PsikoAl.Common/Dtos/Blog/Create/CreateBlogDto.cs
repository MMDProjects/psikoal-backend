namespace PsikoAl.Common.Dtos.Blog.Create;

public sealed record CreateBlogDto(
    string Title,
    string? Slug,
    string Excerpt,
    string Content,
    Guid? CategoryId,
    IReadOnlyList<string> Tags,
    string AuthorName,
    string AuthorTitle);
