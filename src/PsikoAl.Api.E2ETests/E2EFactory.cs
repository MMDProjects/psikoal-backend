using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace PsikoAl.Api.E2ETests;

/// <summary>
/// API'yi GERÇEK lokal Supabase stack'ine bağlar (Postgres + GoTrue).
/// </summary>
/// <remarks>
/// ContractTests'teki ApiFactory'den farkı ve varlık sebebi:
/// ApiFactory `ConnectionStrings:Postgres` ENJEKTE ETMEZ; Program.cs:35-39 boş
/// connection string'de Npgsql provider'ını hiç konfigüre etmez. Yani ContractTests
/// veritabanısız koşar ve yalnız 401 doğrulayabilir. "Admin ekledi -> mobil ucunda
/// göründü" zincirinin gerçek DB'ye ihtiyacı var; bu fabrika onu sağlar.
/// </remarks>
public sealed class E2EFactory : WebApplicationFactory<Program>
{
    // Sahte PayTR mağaza bilgileri: get-token ucu E2E'de ÇAĞRILMAZ (gerçek PayTR'ye gider);
    // bunlar yalnız callback hash'ini testte üretip API'ye doğrulatmak için.
    public const string PayTrMerchantId = "E2E000";
    public const string PayTrMerchantKey = "e2e-merchant-key";
    public const string PayTrMerchantSalt = "e2e-merchant-salt";

    protected override IHost CreateHost(IHostBuilder builder)
    {
        builder.ConfigureHostConfiguration(configuration =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Supabase:Url"] = LocalStack.SupabaseUrl,
                ["Supabase:AnonKey"] = LocalStack.AnonKey,
                ["Supabase:ServiceRoleKey"] = LocalStack.ServiceRoleKey,
                ["ConnectionStrings:Postgres"] = LocalStack.PostgresConnectionString,
                ["PayTR:MerchantId"] = PayTrMerchantId,
                ["PayTR:MerchantKey"] = PayTrMerchantKey,
                ["PayTR:MerchantSalt"] = PayTrMerchantSalt,
                ["PayTR:TestMode"] = "true",
            });
        });

        builder.ConfigureServices(services =>
        {
            // Lokal GoTrue http konuşur. Authority http şemasındayken JwtBearer'ın
            // RequireHttpsMetadata kontrolü OIDC discovery'yi reddeder ve /health dahil
            // TÜM istekler 500'e düşer (ApiFactory bu yüzden sahte bir https URL'i
            // kullanıyor). Burada gerçek token doğrulaması gerektiği için sahte URL bir
            // seçenek değil; kontrolü yalnız test host'unda gevşetiyoruz.
            //
            // Configure, PostConfigure DEĞİL — ve bu ayrım kritik: kontrolü fırlatan
            // kodun kendisi (JwtBearerPostConfigureOptions) bir PostConfigure adımı.
            // PostConfigure'lar kayıt sırasına göre koşar, bizimki sonradan kaydedildiği
            // için ONDAN SONRA çalışırdı ve istisna zaten atılmış olurdu. Configure
            // adımlarının tamamı PostConfigure'lardan önce koşar.
            services.Configure<JwtBearerOptions>(
                JwtBearerDefaults.AuthenticationScheme,
                options => options.RequireHttpsMetadata = false);
        });

        return base.CreateHost(builder);
    }
}
