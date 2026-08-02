namespace PsikoAl.Common.Dtos.Auth;

public sealed record AuthTokensDto(string AccessToken, string RefreshToken, int ExpiresIn);
