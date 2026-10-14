namespace PsikoAl.Common.Dtos.Admin;

public sealed record AdminAuditLogQueryDto(
    string? Actor,
    string? Action,
    string? EntityType,
    string? EntityId,
    DateOnly? From,
    DateOnly? To,
    int? Page,
    int? PerPage);
