using System.Net;
using System.Text;
using PsikoAl.Services;

namespace PsikoAl.Services.Tests;

// İmzalı belge bağlantısı, admin panelinin uzman onay kuyruğunda CV ve sertifikayı
// açan tek yol. Bir dönem bu bağlantı BOZUK üretiliyordu:
//   http://.../storage/v1object/sign/documents/...
//                            ^ eğik çizgi yok
// Supabase'in döndürdüğü signedURL "/object/sign/..." biçiminde ve koddaki
// TrimStart('/') baştaki çizgiyi siliyordu; yeniden konmadığı için "v1" ile
// "object" birleşiyordu.
//
// Belirti neden sinsi: uç 200 döner ve imzalı bağlantı ÜRETİLMİŞ görünür. Hata
// ancak admin bağlantıya tıkladığında 404 olarak ortaya çıkar — yani sunucu
// tarafında hiçbir iz bırakmaz.
public sealed class SupabaseStorageServiceTests
{
    private const string SignedPath =
        "/object/sign/documents/3f1b2c4d/cv.pdf?token=eyJhbGciOiJIUzI1NiJ9.test";

    [Fact]
    public async Task CreateSignedUrlAsync_BuildsAnAbsoluteUrlWithoutLosingTheSeparator()
    {
        var service = CreateService($"{{\"signedURL\":\"{SignedPath}\"}}");

        var url = await service.CreateSignedUrlAsync(
            "documents",
            "3f1b2c4d/cv.pdf",
            TimeSpan.FromMinutes(15),
            CancellationToken.None);

        Assert.Equal(
            "http://127.0.0.1:54321/storage/v1/object/sign/documents/3f1b2c4d/cv.pdf"
            + "?token=eyJhbGciOiJIUzI1NiJ9.test",
            url);

        // Asıl kırılmanın imzası: "v1object". Yukarıdaki tam eşitlik zaten yeterli,
        // ama bu iddia kırmızı testin SEBEBİNİ tek satırda söylüyor.
        Assert.DoesNotContain("v1object", url, StringComparison.Ordinal);

        // Çift eğik çizgi de olmamalı: düzeltme "çizgiyi iki kez ekle"ye kaymasın.
        Assert.DoesNotContain("v1//object", url, StringComparison.Ordinal);
    }

    /// <summary>Storage reddederse DomainException fırlar — çağıran buna güveniyor.</summary>
    /// <remarks>
    /// AdminExpertsController her belgeyi ayrı imzalıyor ve DomainException'ı
    /// yakalayıp o belgeyi atlıyor. Buradaki tür değişirse o dayanıklılık sessizce
    /// devre dışı kalır ve tek bozuk sertifika yine TÜM isteği düşürür.
    /// </remarks>
    [Fact]
    public async Task CreateSignedUrlAsync_ThrowsDomainExceptionWhenStorageRejectsThePath()
    {
        var service = CreateService(
            "{\"statusCode\":\"400\",\"error\":\"InvalidKey\"}",
            HttpStatusCode.BadRequest);

        var exception = await Assert.ThrowsAsync<Common.Exceptions.DomainException>(
            () => service.CreateSignedUrlAsync(
                "documents",
                "BDT Uygulayıcı Sertifikası",
                TimeSpan.FromMinutes(15),
                CancellationToken.None));

        Assert.Equal(Common.Constants.ErrorKeys.StorageUploadFailed, exception.ErrorKey);
    }

    // Herkese açık bağlantı istemcinin ulaşabildiği adresten kurulur; API'nin Supabase'e
    // konuştuğu iç adres (127.0.0.1) telefonda açılmaz. PublicUrl boşsa davranış değişmez.
    [Theory]
    [InlineData(null, "http://127.0.0.1:54321/storage/v1/object/public/blog-media/a/kapak.png")]
    [InlineData("https://dosya.example.com/", "https://dosya.example.com/storage/v1/object/public/blog-media/a/kapak.png")]
    [InlineData("https://dosya.example.com", "https://dosya.example.com/storage/v1/object/public/blog-media/a/kapak.png")]
    public void GetPublicUrl_UsesThePublicBaseWhenConfigured(string? publicUrl, string expected)
    {
        var options = Microsoft.Extensions.Options.Options.Create(new Options.SupabaseOptions
        {
            Url = "http://127.0.0.1:54321",
            AnonKey = "anon",
            ServiceRoleKey = "service",
            PublicUrl = publicUrl,
        });
        var httpClient = new HttpClient(new StubHandler("{}", HttpStatusCode.OK))
        {
            BaseAddress = new Uri("http://127.0.0.1:54321/"),
        };

        var url = new SupabaseStorageService(httpClient, options).GetPublicUrl("blog-media", "a/kapak.png");

        Assert.Equal(expected, url);
    }

    private static SupabaseStorageService CreateService(
        string responseBody,
        HttpStatusCode status = HttpStatusCode.OK)
    {
        var httpClient = new HttpClient(new StubHandler(responseBody, status))
        {
            // Sonda eğik çizgi VAR: HttpClient.BaseAddress'in gerçek biçimi bu ve
            // birleştirme hatası tam olarak burada doğuyor.
            BaseAddress = new Uri("http://127.0.0.1:54321/"),
        };

        return new SupabaseStorageService(httpClient);
    }

    private sealed class StubHandler(string body, HttpStatusCode status) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
            => Task.FromResult(new HttpResponseMessage(status)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json"),
            });
    }
}
