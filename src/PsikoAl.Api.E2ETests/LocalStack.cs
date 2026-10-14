namespace PsikoAl.Api.E2ETests;

/// <summary>
/// Lokal Supabase stack'inin (supabase start) adresleri ve anahtarları.
/// </summary>
/// <remarks>
/// Değerler ortam değişkenlerinden okunur; varsayılanlar Supabase CLI'ın sabit lokal
/// portlarıdır. Anahtarların varsayılanı YOKTUR ve olamaz: CLI sürümleri arasında
/// anahtar formatı değişti (legacy JWT -> sb_publishable_/sb_secret_). Sabit bir
/// varsayılan yazılsaydı, CLI güncellendiği gün testler "401" diye patlar ve hata
/// gerçek bir sözleşme kırılması sanılırdı. Anahtarlar `supabase status -o env`
/// çıktısından beslenir — bkz. scripts/e2e-env.sh.
/// </remarks>
internal static class LocalStack
{
    public static string SupabaseUrl =>
        Environment.GetEnvironmentVariable("E2E_SUPABASE_URL") ?? "http://127.0.0.1:54321";

    public static string AnonKey => Required("E2E_SUPABASE_ANON_KEY");

    public static string ServiceRoleKey => Required("E2E_SUPABASE_SERVICE_ROLE_KEY");

    public static string PostgresConnectionString =>
        Environment.GetEnvironmentVariable("E2E_POSTGRES")
        ?? "Host=127.0.0.1;Port=54322;Database=postgres;Username=postgres;Password=postgres";

    private static string Required(string name)
        => Environment.GetEnvironmentVariable(name) is { Length: > 0 } value
            ? value
            : throw new InvalidOperationException(
                $"""
                 E2E ortam değişkeni tanımsız: {name}

                 Lokal Supabase stack'i ayakta değil ya da ortam beslenmemiş.
                 Çözüm:
                     npx supabase start
                     source scripts/e2e-env.sh      # PowerShell: . ./scripts/e2e-env.ps1
                     dotnet test src/PsikoAl.Api.E2ETests

                 Bu hata BİLEREK fırlatılıyor: testin sessizce atlanması (Skip)
                 yeşil bir CI ile 'zincir doğrulandı' yanılsaması üretirdi.
                 """);
}
