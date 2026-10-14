namespace PsikoAl.Services.Payments;

public sealed record PayTrTokenResult(string? Token, string? FailureReason)
{
    public bool Succeeded => !string.IsNullOrWhiteSpace(Token);

    public static PayTrTokenResult Success(string token) => new(token, null);

    public static PayTrTokenResult Failure(string reason) => new(null, reason);
}
