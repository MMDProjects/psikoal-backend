namespace PsikoAl.Common.Exceptions;

public sealed class DomainException(string errorKey, string? field = null) : Exception(errorKey)
{
    public string ErrorKey { get; } = errorKey;

    public string? Field { get; } = field;
}
