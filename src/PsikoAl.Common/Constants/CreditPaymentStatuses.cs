namespace PsikoAl.Common.Constants;

public static class CreditPaymentStatuses
{
    public const string Pending = "PENDING";
    public const string Paid = "PAID";
    public const string Failed = "FAILED";

    public static readonly IReadOnlyList<string> All = [Pending, Paid, Failed];
}
