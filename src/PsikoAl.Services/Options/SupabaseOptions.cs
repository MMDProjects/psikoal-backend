namespace PsikoAl.Services.Options;

public sealed class SupabaseOptions
{
    public const string SectionName = "Supabase";

    public required string Url { get; set; }

    public required string AnonKey { get; set; }

    public required string ServiceRoleKey { get; set; }

    // Herkese açık dosya (avatar, blog kapağı) bağlantılarının kökü. Boşsa Url kullanılır.
    // API Supabase'e iç adresten (ör. lokal 127.0.0.1) konuşurken istemcinin göreceği
    // adres farklıysa (tünel, CDN, özel alan adı) verilir.
    public string? PublicUrl { get; set; }
}
