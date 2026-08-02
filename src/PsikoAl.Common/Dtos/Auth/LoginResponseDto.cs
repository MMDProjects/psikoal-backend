namespace PsikoAl.Common.Dtos.Auth;

public sealed record LoginResponseDto(AuthUserDto User, AuthTokensDto Tokens);
