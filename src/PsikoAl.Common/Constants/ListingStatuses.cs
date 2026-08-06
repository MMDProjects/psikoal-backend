namespace PsikoAl.Common.Constants;

public static class ListingStatuses
{
    public const string PendingApproval = "PENDING_APPROVAL";
    public const string Open = "OPEN";
    public const string Matched = "MATCHED";
    public const string Closed = "CLOSED";
    public const string Expired = "EXPIRED";
    public const string RejectedByAdmin = "REJECTED_BY_ADMIN";

    public static readonly IReadOnlyList<string> All =
        [PendingApproval, Open, Matched, Closed, Expired, RejectedByAdmin];

    public static readonly IReadOnlyList<string> Active = [PendingApproval, Open];
}
