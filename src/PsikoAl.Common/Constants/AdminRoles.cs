namespace PsikoAl.Common.Constants;

public static class AdminRoles
{
    public const string SuperAdmin = "super_admin";
    public const string Moderator = "moderator";
    public const string ContentEditor = "content_editor";
    public const string Finance = "finance";

    public static readonly IReadOnlyList<string> All = [SuperAdmin, Moderator, ContentEditor, Finance];
}
