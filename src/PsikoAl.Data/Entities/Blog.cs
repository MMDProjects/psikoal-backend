using PsikoAl.Common.Constants;

namespace PsikoAl.Data.Entities;

public sealed class Blog
{
    public Guid Id { get; set; }

    public required string Slug { get; set; }

    public required string Title { get; set; }

    public string Excerpt { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;

    public string? CoverImagePath { get; set; }

    public Guid? CategoryId { get; set; }

    public List<string> Tags { get; set; } = [];

    public required string AuthorName { get; set; }

    public string AuthorTitle { get; set; } = string.Empty;

    public int ReadingMinutes { get; set; } = 1;

    public string Status { get; set; } = BlogStatuses.Draft;

    public DateTimeOffset? PublishedAt { get; set; }

    public Guid? CreatedBy { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public Category? Category { get; set; }

    public List<BlogLike> Likes { get; set; } = [];
}
