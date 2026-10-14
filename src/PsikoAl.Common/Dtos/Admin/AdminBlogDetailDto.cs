namespace PsikoAl.Common.Dtos.Admin;

public sealed record AdminBlogDetailDto(
    Guid Id,
    string Slug,
    string Title,
    string Excerpt,
    string Content,
    string? CoverImageUrl,
    Guid? CategoryId,
    IReadOnlyList<string> Tags,
    string AuthorName,
    string AuthorTitle,
    int ReadingTime,
    string Status,
    bool IsLive,
    DateTimeOffset? PublishedAt,
    int LikeCount,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
