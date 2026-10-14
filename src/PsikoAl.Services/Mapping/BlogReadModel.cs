namespace PsikoAl.Services.Mapping;

public sealed record BlogReadModel(
    Guid Id,
    string Slug,
    string Title,
    string Excerpt,
    string Content,
    string CoverImagePath,
    string? CategoryBlogTag,
    IReadOnlyList<string> Tags,
    string AuthorName,
    string AuthorTitle,
    int ReadingMinutes,
    DateTimeOffset PublishedAt,
    int LikeCount,
    bool Liked);
