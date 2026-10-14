using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace PsikoAl.Api.E2ETests;

/// <summary>
/// "Admin panelin açılış sayfası gerçekten yükleniyor mu?" sorusunun makine cevabı.
/// </summary>
/// <remarks>
/// Bu ucun HİÇ testi yoktu ve tamamen kırıktı: <c>/admin/dashboard/stats</c> 500
/// dönüyordu çünkü <c>GroupBy(x =&gt; x.CreatedAt.UtcDateTime.Date)</c> Npgsql
/// tarafından SQL'e çevrilemiyordu ("No coercion operator is defined between types
/// 'DateTimeOffset' and 'Nullable&lt;DateTime&gt;'").
///
/// Belirti neden fark edilmedi: Blazor sayfası hatayı yakalayıp BOŞ tablo
/// gösteriyor. Yani panelin ilk açılan sayfası sessizce boştu — ne konsol hatası,
/// ne kırmızı bir uyarı. Bu test o sessizliği kapatıyor.
///
/// Contract testleri bu ucu göremezdi: onlar veritabanısız koşuyor ve yalnız 401
/// doğrulayabiliyor. Hata TAM OLARAK sorgu çevirisinde, yani ancak gerçek
/// Postgres'e karşı ortaya çıkıyor.
/// </remarks>
[Trait("Category", "E2E")]
[Collection(E2ECollection.Name)]
public sealed class DashboardChainTests(E2EFixture fixture)
{
    /// <summary>Mobil/panel sözleşmesindeki sayaç alanlarının tamamı.</summary>
    /// <remarks>
    /// Kaynak: AdminDashboardStatsDto. Alan adı değişirse panel o kartı boş
    /// gösterir ama hata vermez — bu dizi o sessiz kırılmayı yakalıyor.
    /// </remarks>
    private static readonly string[] CounterFields =
    [
        "pendingListings", "pendingExperts", "pendingReviews",
        "todayNewClients", "todayNewExperts", "todayNewListings",
        "todayNewOffers", "todayNewMatches", "todayCompletedAssessments",
        "totalUsers", "openListings", "activeMatches",
    ];

    [Fact]
    public async Task Dashboard_sayaçları_gerçek_Postgres_üzerinde_yüklenir()
    {
        var response = await fixture.Admin.GetAsync("/admin/dashboard/stats");

        // Durum kodu ÖNCE ve gövdeyle birlikte iddia ediliyor: 500'ün gövdesi
        // (Npgsql çeviri hatası) teşhisin tamamıydı, yutulmamalı.
        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var stats = await response.Content.ReadFromJsonAsync<JsonElement>();

        foreach (var field in CounterFields)
        {
            Assert.True(stats.TryGetProperty(field, out var value), $"'{field}' alanı yok.");
            Assert.Equal(JsonValueKind.Number, value.ValueKind);
            Assert.True(value.GetInt32() >= 0, $"'{field}' negatif olamaz.");
        }
    }

    /// <summary>
    /// Trend serisi: kırılan sorgu tam olarak buydu.
    /// </summary>
    /// <remarks>
    /// Sayaçlar basit <c>CountAsync</c> çağrıları; tek başına onları doğrulamak
    /// hatayı kaçırırdı. Kıran şey GÜNE GÖRE GRUPLAMAydı, bu yüzden serinin
    /// kendisi ayrıca ölçülüyor.
    /// </remarks>
    [Fact]
    public async Task Dashboard_günlük_trend_serisi_eksiksiz_ve_sıralı_gelir()
    {
        var response = await fixture.Admin.GetAsync("/admin/dashboard/stats");
        response.EnsureSuccessStatusCode();

        var stats = await response.Content.ReadFromJsonAsync<JsonElement>();
        var days = stats.GetProperty("lastDays").EnumerateArray().ToList();

        Assert.NotEmpty(days);

        DateOnly? previous = null;
        foreach (var day in days)
        {
            var date = DateOnly.Parse(day.GetProperty("date").GetString()!, CultureInfo.InvariantCulture);

            // Artan sırada ve BOŞLUKSUZ olmalı: panel bu diziyi olduğu gibi
            // çiziyor, eksik gün grafiği sessizce kaydırırdı.
            if (previous is { } earlier)
            {
                Assert.Equal(earlier.AddDays(1), date);
            }

            Assert.True(day.GetProperty("newListings").GetInt32() >= 0);
            Assert.True(day.GetProperty("newMatches").GetInt32() >= 0);
            previous = date;
        }

        // Seri BUGÜNDE bitmeli. Bitmezse panel "bugün" kolonunu hiç göstermez ve
        // yönetici dünün verisine bakıp güncel sanır.
        Assert.Equal(
            DateOnly.FromDateTime(DateTime.UtcNow),
            DateOnly.Parse(days[^1].GetProperty("date").GetString()!, CultureInfo.InvariantCulture));
    }

    /// <summary>
    /// NEGATİF: danışan token'ı dashboard'a erişemez.
    /// </summary>
    /// <remarks>
    /// Geçerli bir token'la ölçülüyor — token'sız istekten daha güçlü kanıt,
    /// çünkü kimlik doğrulamanın değil YETKİLENDİRMENİN çalıştığını gösteriyor.
    /// Dashboard tüm platformun sayaçlarını döküyor; sızması ciddi olurdu.
    /// </remarks>
    [Fact]
    public async Task Danışan_token_ile_dashboard_okunamaz()
    {
        var response = await fixture.Client.GetAsync("/admin/dashboard/stats");

        Assert.NotEqual(HttpStatusCode.OK, response.StatusCode);
    }
}
