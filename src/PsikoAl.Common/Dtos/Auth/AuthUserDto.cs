namespace PsikoAl.Common.Dtos.Auth;

public sealed record AuthUserDto(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    string Role,
    bool IsVerified,
    string? AvatarUrl,
    DateTimeOffset CreatedAt,
    string? Phone,
    string? City);
