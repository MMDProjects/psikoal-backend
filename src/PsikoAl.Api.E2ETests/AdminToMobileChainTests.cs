using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace PsikoAl.Api.E2ETests;

/// <summary>
/// "Admin panelden eklediğimde mobil uygulamada geliyor mu?" sorusunun makine cevabı.
/// </summary>
/// <remarks>
/// Zincir: admin token -> POST /admin/categories -> Postgres -> GET /categories ->
/// mobil uygulamanın Zod şeması. Halkalardan biri kopsa bu testler kırmızı olur.
/// Kategori bilinçli seçildi: mobil taraftaki sözleşmesi (CategorySchema) sabit,
/// yan etkisi yok ve admin ucu ile mobil ucu AYNI tabloyu farklı filtreyle okuyor
/// (AdminCategoryService.ListAllAsync tümünü, CategoryService.ListActiveAsync yalnız
/// is_active olanları) — yani "eklendi ama görünmüyor" hatasının doğal yuvası.
/// </remarks>
/// <remarks>
/// <c>Category=E2E</c> trait'i şart: mevcut <c>build-test</c> job'u
/// <c>dotnet test src/PsikoAl.slnx</c> koşuyor ve bu proje solution'a dahil.
/// Trait olmadan CI'daki birim test adımı lokal Supabase stack'i olmadığı için
/// kırmızı olurdu. Birim job'u <c>--filter "Category!=E2E"</c> ile dışlar,
/// e2e.yml ise <c>--filter "Category=E2E"</c> ile yalnız bunları koşar.
/// </remarks>
[Trait("Category", "E2E")]
[Collection(E2ECollection.Name)]
public sealed class AdminToMobileChainTests(E2EFixture fixture)
{
    [Fact]
    public async Task Admin_eklediği_kategori_mobil_kategori_listesinde_görünür()
    {
        var slug = fixture.NewCategorySlug();

        var created = await CreateCategoryAsync(slug);
        Assert.Equal(HttpStatusCode.OK, created.StatusCode);

        var categories = await FetchMobileCategoriesAsync();

        Assert.Contains(
            categories,
            category => category.GetProperty("slug").GetString() == slug);
    }

    [Fact]
    public async Task Admin_pasifleştirdiği_kategori_mobil_listesinden_düşer()
    {
        var slug = fixture.NewCategorySlug();

        var created = await CreateCategoryAsync(slug);
        created.EnsureSuccessStatusCode();
        var body = await created.Content.ReadFromJsonAsync<JsonElement>();
        var id = body.GetProperty("id").GetGuid();

        var patch = await fixture.Admin.PatchAsJsonAsync(
            $"/admin/categories/{id}",
            new { isActive = false });
        patch.EnsureSuccessStatusCode();

        var categories = await FetchMobileCategoriesAsync();

        Assert.DoesNotContain(
            categories,
            category => category.GetProperty("slug").GetString() == slug);
    }

    /// <summary>
    /// Mobil uçtaki yanıt gövdesi, uygulamanın Zod şemasıyla alan alan aynı olmalı.
    /// </summary>
    /// <remarks>
    /// Kaynak: native-atomic/src/domains/category/schemas/category.schema.ts ve
    /// native-atomic/src/domains/category/api/useCategoriesQuery.ts.
    /// Alan adı veya sarmal değişirse mobil taraf `safeParse` hatasıyla BOŞ LİSTE
    /// gösterir — HTTP 200 döndüğü için hiçbir status-kodu testi bunu yakalamaz.
    /// Bu test o boşluğu kapatıyor.
    /// </remarks>
    [Fact]
    public async Task Mobil_kategori_yanıtı_uygulamanın_şemasına_uyar()
    {
        var slug = fixture.NewCategorySlug();
        (await CreateCategoryAsync(slug)).EnsureSuccessStatusCode();

        var raw = await fixture.Mobile.GetFromJsonAsync<JsonElement>("/categories");

        // Sarmal: { data: [...], meta: { page, total, perPage } }
        Assert.Equal(JsonValueKind.Array, raw.GetProperty("data").ValueKind);
        var meta = raw.GetProperty("meta");
        Assert.Equal(JsonValueKind.Number, meta.GetProperty("page").ValueKind);
        Assert.Equal(JsonValueKind.Number, meta.GetProperty("total").ValueKind);
        Assert.Equal(JsonValueKind.Number, meta.GetProperty("perPage").ValueKind);

        var category = raw.GetProperty("data")
            .EnumerateArray()
            .Single(item => item.GetProperty("slug").GetString() == slug);

        foreach (var field in (string[])["id", "slug", "name", "icon", "summary", "description", "blogTag"])
        {
            Assert.Equal(JsonValueKind.String, category.GetProperty(field).ValueKind);
        }

        // assessmentCategory nullable — alan VAR olmak zorunda, değeri null olabilir.
        var assessmentCategory = category.GetProperty("assessmentCategory").ValueKind;
        Assert.True(
            assessmentCategory is JsonValueKind.String or JsonValueKind.Null,
            $"assessmentCategory string|null olmalı, gelen: {assessmentCategory}");
    }

    /// <summary>
    /// Zincirin yeşil olmasının sebebi gerçek token; yetkilendirme kapalı olduğu için değil.
    /// </summary>
    /// <remarks>
    /// Bu test olmadan diğerleri sahte yeşil üretebilirdi: AdminRequirementHandler
    /// yanlışlıkla herkesi geçirse de "ekledim, göründü" testleri geçerdi.
    /// </remarks>
    [Fact]
    public async Task Token_olmadan_admin_ucuna_kategori_eklenemez()
    {
        var response = await fixture.Mobile.PostAsJsonAsync(
            "/admin/categories",
            NewCategoryPayload("e2e-yetkisiz"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    /// <summary>
    /// Danışan token'ı admin ucuna GEÇMEZ — kimliksiz istekten daha güçlü bir kanıt.
    /// </summary>
    /// <remarks>
    /// Tokensız test yalnız "authentication var mı"yı ölçer. Bu test
    /// AUTHORIZATION'ı ölçüyor: geçerli, imzası doğrulanmış bir Supabase token'ı
    /// var ama admin_users satırı yok, dolayısıyla AdminRequirementHandler
    /// requirement'ı Succeed ETMEMELİ. Mobil uygulamanın hesabıyla admin paneli
    /// uçlarına erişilebilmesi sessiz bir yetki açığı olurdu.
    /// </remarks>
    [Fact]
    public async Task Danışan_token_ile_admin_ucuna_erişilemez()
    {
        var response = await fixture.Client.PostAsJsonAsync(
            "/admin/categories",
            NewCategoryPayload("e2e-danisan-denemesi"));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private Task<HttpResponseMessage> CreateCategoryAsync(string slug)
        => fixture.Admin.PostAsJsonAsync("/admin/categories", NewCategoryPayload(slug));

    private static object NewCategoryPayload(string slug) => new
    {
        slug,
        name = $"E2E {slug}",
        icon = "brain",
        summary = "E2E testinin ürettiği geçici kategori.",
        description = "Bu kayıt otomatik testler tarafından üretilir ve silinir.",
        blogTag = slug,
        assessmentCategory = (string?)null,
    };

    private async Task<JsonElement[]> FetchMobileCategoriesAsync()
    {
        var raw = await fixture.Mobile.GetFromJsonAsync<JsonElement>("/categories");
        return [.. raw.GetProperty("data").EnumerateArray()];
    }
}
