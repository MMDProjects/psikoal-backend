namespace PsikoAl.Common.Constants;

public static class ProfileStatuses
{
    public const string Active = "active";
    public const string Frozen = "frozen";
    public const string Deleted = "deleted";

    public static readonly IReadOnlyList<string> All = [Active, Frozen, Deleted];
}
