namespace PsikoAl.Common.Dtos.Admin;

public sealed record AdminAuditLogDto(
    long Id,
    string ActorType,
    Guid? AdminUserId,
    string? AdminDisplayName,
    string Action,
    string EntityType,
    string EntityId,
    string? OldValue,
    string? NewValue,
    string? Reason,
    DateTimeOffset CreatedAt);
