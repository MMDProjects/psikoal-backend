namespace PsikoAl.Common.Dtos.Admin;

public sealed record AdminBlogListItemDto(
    Guid Id,
    string Slug,
    string Title,
    string Status,
    bool IsLive,
    string? CategoryName,
    DateTimeOffset? PublishedAt,
    int LikeCount,
    bool HasCover,
    DateTimeOffset UpdatedAt);
