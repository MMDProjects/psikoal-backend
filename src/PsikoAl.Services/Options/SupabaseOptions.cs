namespace PsikoAl.Services.Options;

public sealed class SupabaseOptions
{
    public const string SectionName = "Supabase";

    public required string Url { get; set; }

    public required string AnonKey { get; set; }

    public required string ServiceRoleKey { get; set; }

    public required string JwtSecret { get; set; }
}
