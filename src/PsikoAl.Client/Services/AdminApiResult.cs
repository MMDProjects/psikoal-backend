namespace PsikoAl.Client.Services;

public record AdminApiResult(string? ErrorMessage)
{
    public static readonly AdminApiResult Success = new((string?)null);

    public bool Succeeded => ErrorMessage is null;

    public static AdminApiResult Failure(string errorMessage) => new(errorMessage);

    public static AdminApiResult<T> Ok<T>(T value) => new(value, null);

    public static AdminApiResult<T> Failure<T>(string errorMessage) => new(default, errorMessage);
}

public sealed record AdminApiResult<T>(T? Value, string? ErrorMessage) : AdminApiResult(ErrorMessage);
