namespace PsikoAl.Common.Constants;

public static class ExpertStatuses
{
    public const string Pending = "pending";
    public const string Approved = "approved";
    public const string Rejected = "rejected";

    public static readonly IReadOnlyList<string> All = [Pending, Approved, Rejected];
}
