namespace PsikoAl.Common.Dtos.Blog.Update;

public sealed record UpdateBlogDto(
    string Title,
    string Slug,
    string Excerpt,
    string Content,
    Guid? CategoryId,
    IReadOnlyList<string> Tags,
    string AuthorName,
    string AuthorTitle);
