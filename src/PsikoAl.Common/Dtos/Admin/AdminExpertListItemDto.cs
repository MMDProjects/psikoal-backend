namespace PsikoAl.Common.Dtos.Admin;

public sealed record AdminExpertListItemDto(
    Guid Id,
    string FullName,
    string Email,
    string Title,
    string Status,
    bool IsVerified,
    bool HasPendingRevision,
    DateTimeOffset CreatedAt);
