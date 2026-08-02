namespace PsikoAl.Common.Dtos;

public sealed record ApiErrorDto(string Code, string Message, string? Field = null);
