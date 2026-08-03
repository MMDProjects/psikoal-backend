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
    public const string ExpertNotFound = "EXPERT_NOT_FOUND";
    public const string ExpertProfileAlreadyExists = "EXPERT_PROFILE_ALREADY_EXISTS";
    public const string ExpertRoleRequired = "EXPERT_ROLE_REQUIRED";
    public const string ExpertRejectionReasonRequired = "EXPERT_REJECTION_REASON_REQUIRED";
    public const string FileTypeNotAllowed = "FILE_TYPE_NOT_ALLOWED";
    public const string FileTooLarge = "FILE_TOO_LARGE";
    public const string StorageUploadFailed = "STORAGE_UPLOAD_FAILED";
    public const string ValidationFailed = "VALIDATION_FAILED";
    public const string InternalError = "INTERNAL_ERROR";
}
