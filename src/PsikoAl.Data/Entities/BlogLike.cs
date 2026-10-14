namespace PsikoAl.Data.Entities;

public sealed class BlogLike
{
    public Guid BlogId { get; set; }

    public Guid UserId { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
