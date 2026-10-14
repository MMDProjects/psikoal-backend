namespace PsikoAl.Common.Constants;

public static class AdminRoleGroups
{
    public static readonly IReadOnlyList<string> Any = AdminRoles.All;

    public static readonly IReadOnlyList<string> SuperAdminOnly = [AdminRoles.SuperAdmin];

    public static readonly IReadOnlyList<string> Moderation = [AdminRoles.SuperAdmin, AdminRoles.Moderator];

    public static readonly IReadOnlyList<string> Content = [AdminRoles.SuperAdmin, AdminRoles.ContentEditor];

    public static readonly IReadOnlyList<string> Finance = [AdminRoles.SuperAdmin, AdminRoles.Finance];

    public static bool Allows(IReadOnlyList<string> allowedRoles, string? role)
        => role is not null && allowedRoles.Contains(role, StringComparer.Ordinal);
}
