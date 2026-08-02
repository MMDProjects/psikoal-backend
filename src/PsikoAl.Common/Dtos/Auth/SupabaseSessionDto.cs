namespace PsikoAl.Common.Dtos.Auth;

public sealed record SupabaseSessionDto(Guid UserId, AuthTokensDto Tokens);
