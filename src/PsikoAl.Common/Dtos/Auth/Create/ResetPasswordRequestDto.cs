namespace PsikoAl.Common.Dtos.Auth.Create;

public sealed record ResetPasswordRequestDto(string AccessToken, string NewPassword);
