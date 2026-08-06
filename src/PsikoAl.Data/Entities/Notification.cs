namespace PsikoAl.Data.Entities;

public sealed class Notification
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public required string Type { get; set; }

    public required string Title { get; set; }

    public required string Body { get; set; }

    public string? Data { get; set; }

    public bool Read { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
