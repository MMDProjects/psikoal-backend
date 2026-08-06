namespace PsikoAl.Common.Constants;

public static class OfferStatuses
{
    public const string Pending = "PENDING";
    public const string Accepted = "ACCEPTED";
    public const string Rejected = "REJECTED";
    public const string Withdrawn = "WITHDRAWN";

    public static readonly IReadOnlyList<string> All = [Pending, Accepted, Rejected, Withdrawn];
}
