namespace PsikoAl.Common.Constants;

public static class ErrorKeys
{
    public const string AuthInvalidCredentials = "AUTH_INVALID_CREDENTIALS";
    public const string AuthEmailAlreadyRegistered = "AUTH_EMAIL_ALREADY_REGISTERED";
    public const string AuthEmailInvalid = "AUTH_EMAIL_INVALID";
    public const string AuthWeakPassword = "AUTH_WEAK_PASSWORD";
    public const string AuthEmailNotConfirmed = "AUTH_EMAIL_NOT_CONFIRMED";
    public const string AuthRateLimited = "AUTH_RATE_LIMITED";
    public const string AuthRefreshTokenInvalid = "AUTH_REFRESH_TOKEN_INVALID";
    public const string AuthUserNotFound = "AUTH_USER_NOT_FOUND";
    public const string AuthAccountFrozen = "AUTH_ACCOUNT_FROZEN";
    public const string AuthProviderError = "AUTH_PROVIDER_ERROR";
    public const string ProfileNotFound = "PROFILE_NOT_FOUND";
    public const string AdminUserNotFound = "ADMIN_USER_NOT_FOUND";
    public const string ValidationFailed = "VALIDATION_FAILED";
    public const string InternalError = "INTERNAL_ERROR";
}
