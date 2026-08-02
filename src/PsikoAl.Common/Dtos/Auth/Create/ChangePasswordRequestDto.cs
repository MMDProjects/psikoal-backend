namespace PsikoAl.Common.Dtos.Auth.Create;

public sealed record ChangePasswordRequestDto(
    string CurrentPassword,
    string NewPassword,
    string ConfirmPassword);
