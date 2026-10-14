namespace PsikoAl.Common.Constants;

public static class BlogStatuses
{
    public const string Draft = "draft";
    public const string Published = "published";
    public const string Scheduled = "scheduled";

    public static readonly IReadOnlyList<string> All = [Draft, Published, Scheduled];
}
