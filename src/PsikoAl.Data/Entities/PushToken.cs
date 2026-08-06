namespace PsikoAl.Data.Entities;

public sealed class PushToken
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public required string Token { get; set; }

    public required string Platform { get; set; }

    public string? DeviceId { get; set; }

    public DateTimeOffset LastSeenAt { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
