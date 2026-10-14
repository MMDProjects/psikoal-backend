using System.Net.Http.Headers;
using Npgsql;

namespace PsikoAl.Api.E2ETests;

/// <summary>
/// Tüm E2E testlerinin paylaştığı kurulum: gerçek API host'u + gerçek admin token'ı.
/// </summary>
/// <remarks>
/// Tek koleksiyon = SERİ koşum. Bu bilinçli: senaryolar aynı Postgres'i ve aynı e2e
/// admin kullanıcısını paylaşıyor. Paralel koşulsalardı GoTrue'nun dakikalık login
/// limitine (Program.cs:150, "auth" politikası 10/dk) takılır ve testler sözleşme
/// değil altyapı yüzünden kırmızı olurdu.
/// </remarks>
public sealed class E2EFixture : IAsyncLifetime, IDisposable
{
    private readonly List<string> _createdCategorySlugs = [];
    private E2EFactory _factory = null!;

    /// <summary>Gerçek admin JWT'si taşıyan istemci — admin panelin yerine geçer.</summary>
    public HttpClient Admin { get; private set; } = null!;

    /// <summary>Token'sız istemci — mobil uygulamanın kimliksiz gördüğü uçlar.</summary>
    public HttpClient Mobile { get; private set; } = null!;

    /// <summary>Danışan token'ı taşıyan istemci — mobil uygulamanın oturumlu uçları.</summary>
    public HttpClient Client { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        _factory = new E2EFactory();

        var admin = await SupabaseTestIdentity.EnsureAdminAsync(CancellationToken.None);

        Admin = _factory.CreateClient();
        Admin.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", admin.AccessToken);

        Mobile = _factory.CreateClient();

        // Danışan hesabı API testleri için şart değil ama Maestro akışları onunla
        // giriş yapıyor. Kurulumu buraya koymak, mobil adımın .NET katmanına
        // bağımlı olmasını sağlıyor — ve o katman CI'da mobil job'undan ÖNCE koşuyor.
        var client = await SupabaseTestIdentity.EnsureClientAsync(CancellationToken.None);
        Client = _factory.CreateClient();
        Client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", client.AccessToken);
    }

    /// <summary>Test sonunda silinmek üzere bir kategori slug'ı kaydeder.</summary>
    public string TrackCategory(string slug)
    {
        _createdCategorySlugs.Add(slug);
        return slug;
    }

    /// <summary>Çakışmayan slug üretir — aynı stack'te art arda koşumlar çarpışmasın.</summary>
    public string NewCategorySlug() => TrackCategory($"e2e-{Guid.NewGuid():N}"[..20]);

    /// <summary>Asenkron kısım: üretilen test verisini DB'den kaldırır.</summary>
    public Task DisposeAsync() => CleanupCategoriesAsync();

    /// <summary>
    /// Senkron kısım: HttpClient'lar ve test host'u.
    /// </summary>
    /// <remarks>
    /// xUnit koleksiyon fixture'ında ÖNCE DisposeAsync, SONRA Dispose koşar. Temizlik
    /// SQL'i bu yüzden DisposeAsync'te: host kapandıktan sonra çalıştırılsaydı sorun
    /// olmazdı ama ayrım CA1001'i de karşılıyor (atılabilir alan = IDisposable).
    /// </remarks>
    public void Dispose()
    {
        Admin?.Dispose();
        Mobile?.Dispose();
        Client?.Dispose();
        _factory?.Dispose();
    }

    private async Task CleanupCategoriesAsync()
    {
        if (_createdCategorySlugs.Count == 0)
        {
            return;
        }

        // Temizlik API üzerinden YAPILAMAZ: kategori silme ucu yok (yalnız pasifleştirme).
        // Satırlar DB'den kaldırılmazsa unique slug/name kısıtı bir sonraki koşumda
        // sessizce 409 üretirdi.
        await using var dataSource = new NpgsqlDataSourceBuilder(LocalStack.PostgresConnectionString).Build();
        await using var connection = await dataSource.OpenConnectionAsync(CancellationToken.None);
        await using var command = connection.CreateCommand();
        command.CommandText = "delete from public.categories where slug = any(@slugs);";
        command.Parameters.AddWithValue("slugs", _createdCategorySlugs.ToArray());
        await command.ExecuteNonQueryAsync(CancellationToken.None);
    }
}

[CollectionDefinition(E2ECollection.Name)]
public sealed class E2ECollection : ICollectionFixture<E2EFixture>
{
    public const string Name = "psikoal-e2e";
}
