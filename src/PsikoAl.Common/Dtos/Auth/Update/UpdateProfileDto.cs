namespace PsikoAl.Common.Dtos.Auth.Update;

public sealed record UpdateProfileDto(
    string? FirstName,
    string? LastName,
    string? Phone,
    string? City,
    bool? ShareEmail,
    bool? SharePhone,
    bool? ShareLocation);
