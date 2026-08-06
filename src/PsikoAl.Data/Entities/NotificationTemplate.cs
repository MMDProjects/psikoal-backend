namespace PsikoAl.Data.Entities;

public sealed class NotificationTemplate
{
    public Guid Id { get; set; }

    public required string Type { get; set; }

    public required string Title { get; set; }

    public required string Body { get; set; }

    public string? HtmlBody { get; set; }

    public bool PushEnabled { get; set; }

    public bool EmailEnabled { get; set; }

    public bool InAppEnabled { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}
