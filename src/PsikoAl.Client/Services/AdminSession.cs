namespace PsikoAl.Client.Services;

public sealed record AdminSession(
    string AccessToken,
    string RefreshToken,
    DateTimeOffset AccessTokenExpiresAt,
    string Email,
    string DisplayName,
    string Role);
