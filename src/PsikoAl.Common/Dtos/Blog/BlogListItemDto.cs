namespace PsikoAl.Common.Dtos.Blog;

public sealed record BlogListItemDto(
    Guid Id,
    string Slug,
    string Title,
    string Excerpt,
    string CoverImage,
    IReadOnlyList<string> Categories,
    int LikeCount,
    bool Liked,
    int ReadingTime,
    DateTimeOffset PublishedAt,
    BlogAuthorDto Author);
