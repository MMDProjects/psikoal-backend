using PsikoAl.Common.Constants;

namespace PsikoAl.Data.Entities;

public sealed class AuditLog
{
    public long Id { get; set; }

    public Guid? AdminUserId { get; set; }

    public string ActorType { get; set; } = AuditActorTypes.Admin;

    public required string Action { get; set; }

    public required string EntityType { get; set; }

    public required string EntityId { get; set; }

    public string? OldValue { get; set; }

    public string? NewValue { get; set; }

    public string? Reason { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
