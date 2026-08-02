namespace PsikoAl.Common.Dtos.Auth.Create;

public sealed record RegisterRequestDto(
    string Email,
    string Password,
    string FirstName,
    string LastName,
    string Role);
