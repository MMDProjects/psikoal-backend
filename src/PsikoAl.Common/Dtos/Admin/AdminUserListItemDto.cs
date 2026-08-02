namespace PsikoAl.Common.Dtos.Admin;

public sealed record AdminUserListItemDto(
    Guid Id,
    string Email,
    string FullName,
    string Role,
    bool IsVerified,
    string Status,
    DateTimeOffset CreatedAt);
