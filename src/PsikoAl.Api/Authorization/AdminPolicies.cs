using Microsoft.AspNetCore.Authorization;
using PsikoAl.Common.Constants;

namespace PsikoAl.Api.Authorization;

// Policy adı → izinli admin_users.role kümesi. Yeni bir admin uç noktası bu
// sabitlerden birini [Authorize(Policy = ...)] ile açıkça seçer:
//   Any         — aktif her admin (genel bakış, kendi oturum bilgisi)
//   SuperAdmin  — sistem ayarları, bildirim gönderimi/şablonları, audit log, admin yönetimi
//   Moderation  — onay kuyrukları (uzman/ilan/yorum), eşleşme müdahalesi, kullanıcı dondurma
//   Content     — CMS: kategoriler, testler, blog
// super_admin her kümede vardır.
public static class AdminPolicies
{
    public const string Any = "Admin";
    public const string SuperAdmin = "AdminSuperAdmin";
    public const string Moderation = "AdminModeration";
    public const string Content = "AdminContent";

    public static readonly IReadOnlyDictionary<string, IReadOnlyList<string>> RolesByPolicy =
        new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal)
        {
            [Any] = AdminRoleGroups.Any,
            [SuperAdmin] = AdminRoleGroups.SuperAdminOnly,
            [Moderation] = AdminRoleGroups.Moderation,
            [Content] = AdminRoleGroups.Content,
        };

    public static void AddAdminPolicies(this AuthorizationOptions options)
    {
        foreach (var (policyName, roles) in RolesByPolicy)
        {
            options.AddPolicy(policyName, policy => policy
                .RequireAuthenticatedUser()
                .AddRequirements(new AdminRequirement(roles)));
        }
    }
}
