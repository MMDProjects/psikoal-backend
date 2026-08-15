namespace PsikoAl.Client.Components.Layout;

public sealed record AdminNavItem(string Href, string Label, string Icon);

public static class AdminNavigation
{
    public static IReadOnlyList<AdminNavItem> Items { get; } =
    [
        new("/", "Dashboard", "dashboard"),
        new("/users", "Kullanıcılar", "users"),
        new("/experts", "Uzmanlar", "expert"),
        new("/listings", "İlanlar", "listing"),
        new("/reviews", "Yorumlar", "review"),
        new("/matches", "Eşleşmeler", "match"),
        new("/categories", "Kategoriler", "category"),
        new("/assessments", "Testler", "assessment"),
        new("/notifications", "Bildirimler", "notification"),
        new("/settings", "Ayarlar", "settings"),
    ];

    public static string TitleFor(string relativePath)
    {
        var normalized = "/" + relativePath.Trim('/');
        var match = Items.FirstOrDefault(item => item.Href == normalized);
        return match?.Label ?? "PsikoAl Admin";
    }
}
