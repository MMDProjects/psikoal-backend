namespace PsikoAl.Common.Constants;

public static class MatchStatuses
{
    public const string Active = "ACTIVE";
    public const string Completed = "COMPLETED";
    public const string Released = "RELEASED";

    public static readonly IReadOnlyList<string> All = [Active, Completed, Released];
    public static readonly IReadOnlyList<string> Past = [Completed, Released];
}
