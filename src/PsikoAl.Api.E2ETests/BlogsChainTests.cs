using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Npgsql;

namespace PsikoAl.Api.E2ETests;

/// <summary>
/// "Admin panelde yayınladığım yazı uygulamada görünüyor, taslak ya da saati
/// gelmemiş yazı görünmüyor mu?" sorusunun makine cevabı.
/// </summary>
/// <remarks>
/// Zincir: admin token -> POST /admin/blogs (+ kapak, yayın/plan) -> Postgres ->
/// GET /blogs, GET /blogs/{slug}, POST /blogs/{slug}/like -> mobil uygulamanın Zod
/// şeması (native-atomic/src/domains/blog/schemas/blog.schema.ts).
/// Admin listesi ile mobil liste AYNI tabloyu FARKLI kuralla okuyor
/// (AdminBlogService filtresiz, BlogService BlogVisibility.IsPublicAt) — taslak
/// sızıntısının doğal yuvası ve sızıntı hiçbir HTTP status koduyla belli olmaz.
/// </remarks>
/// <remarks>
/// Her test kendi yazısını, benzersiz bir etiketle açar: GET /blogs?category=etiket
/// yalnız bu yazıyı döndürür, seed yazıları sayımları bozmaz.
/// </remarks>
[Trait("Category", "E2E")]
[Collection(E2ECollection.Name)]
public sealed class BlogsChainTests(E2EFixture fixture) : IAsyncLifetime
{
    // 1x1 PNG: imza (89 50 4E 47) magic-byte doğrulamasını geçer, bucket'ın
    // allowed_mime_types listesinde image/png var.
    private static readonly byte[] TinyPng = Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg==");

    private readonly string _tag = $"e2e{Guid.NewGuid():N}"[..14];
    private readonly List<Guid> _createdBlogIds = [];

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        foreach (var id in _createdBlogIds)
        {
            await fixture.Admin.DeleteAsync($"/admin/blogs/{id}");
        }
    }

    [Fact]
    public async Task Taslak_yazı_mobilde_görünmez_yayınlanınca_şemaya_uygun_görünür()
    {
        var blog = await CreateBlogWithCoverAsync();
        var slug = blog.GetProperty("slug").GetString() ?? string.Empty;

        var draftList = await FetchMobileListAsync(fixture.Mobile);
        Assert.Equal(0, draftList.GetProperty("meta").GetProperty("total").GetInt32());
        var draftDetail = await fixture.Mobile.GetAsync($"/blogs/{slug}");
        Assert.NotEqual(HttpStatusCode.OK, draftDetail.StatusCode);

        var publish = await fixture.Admin.PostAsJsonAsync($"/admin/blogs/{BlogId(blog)}/publish", new { });
        publish.EnsureSuccessStatusCode();

        var list = await FetchMobileListAsync(fixture.Mobile);
        var item = Assert.Single(list.GetProperty("data").EnumerateArray());
        Assert.Equal(slug, item.GetProperty("slug").GetString());
        Assert.False(item.TryGetProperty("content", out _));
        AssertZodShape(item);

        // Kategori sayfası GET /blogs?category={category.blogTag} çağırıyor: birincil
        // kategorinin blogTag'i categories dizisinin İLK elemanı olmalı.
        var categories = item.GetProperty("categories").EnumerateArray().Select(c => c.GetString()).ToList();
        Assert.Equal("anksiyete", categories[0]);
        Assert.Contains(_tag, categories);

        var detail = await fixture.Mobile.GetFromJsonAsync<JsonElement>($"/blogs/{slug}");
        AssertZodShape(detail);
        Assert.Equal(BlogContent, detail.GetProperty("content").GetString());
        Assert.False(detail.GetProperty("liked").GetBoolean());
    }

    [Fact]
    public async Task Planlanan_yazı_saati_gelene_kadar_gizli_kalır()
    {
        var blog = await CreateBlogWithCoverAsync();
        var id = BlogId(blog);

        var schedule = await fixture.Admin.PostAsJsonAsync(
            $"/admin/blogs/{id}/schedule",
            new { publishAt = DateTimeOffset.UtcNow.AddDays(2) });
        schedule.EnsureSuccessStatusCode();

        var hidden = await FetchMobileListAsync(fixture.Mobile);
        Assert.Equal(0, hidden.GetProperty("meta").GetProperty("total").GetInt32());

        // Cron yok: görünürlük sorgu anında published_at <= now() ile karar veriyor.
        // Saatin geldiğini taklit etmek için yalnız tarihi geriye çekiyoruz.
        await SetPublishedAtAsync(id, DateTimeOffset.UtcNow.AddMinutes(-1));

        var visible = await FetchMobileListAsync(fixture.Mobile);
        Assert.Single(visible.GetProperty("data").EnumerateArray());

        var adminItem = (await fixture.Admin.GetFromJsonAsync<JsonElement>("/admin/blogs?status=scheduled"))
            .EnumerateArray()
            .Single(row => row.GetProperty("id").GetGuid() == id);
        Assert.True(adminItem.GetProperty("isLive").GetBoolean());
    }

    [Fact]
    public async Task Beğeni_toggle_çalışır_ve_oturum_ister()
    {
        var blog = await CreateBlogWithCoverAsync();
        var slug = blog.GetProperty("slug").GetString() ?? string.Empty;
        (await fixture.Admin.PostAsJsonAsync($"/admin/blogs/{BlogId(blog)}/publish", new { })).EnsureSuccessStatusCode();

        var anonymous = await fixture.Mobile.PostAsJsonAsync($"/blogs/{slug}/like", new { });
        Assert.Equal(HttpStatusCode.Unauthorized, anonymous.StatusCode);

        var first = await LikeAsync(slug);
        Assert.True(first.GetProperty("liked").GetBoolean());
        Assert.Equal(1, first.GetProperty("likeCount").GetInt32());

        var detail = await fixture.Client.GetFromJsonAsync<JsonElement>($"/blogs/{slug}");
        Assert.True(detail.GetProperty("liked").GetBoolean());
        Assert.Equal(1, detail.GetProperty("likeCount").GetInt32());

        var second = await LikeAsync(slug);
        Assert.False(second.GetProperty("liked").GetBoolean());
        Assert.Equal(0, second.GetProperty("likeCount").GetInt32());
    }

    [Fact]
    public async Task Yayından_kaldırılan_yazı_mobilden_düşer_admin_listesinde_kalır_ve_audit_yazılır()
    {
        var blog = await CreateBlogWithCoverAsync();
        var id = BlogId(blog);
        (await fixture.Admin.PostAsJsonAsync($"/admin/blogs/{id}/publish", new { })).EnsureSuccessStatusCode();
        Assert.Single((await FetchMobileListAsync(fixture.Mobile)).GetProperty("data").EnumerateArray());

        (await fixture.Admin.PostAsJsonAsync($"/admin/blogs/{id}/unpublish", new { })).EnsureSuccessStatusCode();

        Assert.Empty((await FetchMobileListAsync(fixture.Mobile)).GetProperty("data").EnumerateArray());

        var adminItem = (await fixture.Admin.GetFromJsonAsync<JsonElement>("/admin/blogs"))
            .EnumerateArray()
            .Single(row => row.GetProperty("id").GetGuid() == id);
        Assert.Equal("draft", adminItem.GetProperty("status").GetString());

        var actions = await ReadAuditActionsAsync(id);
        Assert.Equal(
            ["admin.blog_create", "admin.blog_cover", "admin.blog_publish", "admin.blog_unpublish"],
            actions);
    }

    [Fact]
    public async Task Kapaksız_yazı_yayınlanamaz()
    {
        var create = await fixture.Admin.PostAsJsonAsync("/admin/blogs", NewBlogBody());
        create.EnsureSuccessStatusCode();
        var blog = await create.Content.ReadFromJsonAsync<JsonElement>();
        _createdBlogIds.Add(BlogId(blog));

        var publish = await fixture.Admin.PostAsJsonAsync($"/admin/blogs/{BlogId(blog)}/publish", new { });

        Assert.False(publish.IsSuccessStatusCode);
        var error = await publish.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("BLOG_NOT_PUBLISHABLE", error.GetProperty("code").GetString());
    }

    private const string BlogContent = "Birinci paragraf: nefes egzersizi.\n\nİkinci paragraf: bilişsel yeniden çerçeveleme.";

    private static Guid BlogId(JsonElement blog) => blog.GetProperty("id").GetGuid();

    private object NewBlogBody()
        => new
        {
            title = $"E2E Blog {_tag} Çalışması",
            slug = (string?)null,
            excerpt = "E2E testinin ürettiği geçici blog yazısı.",
            content = BlogContent,
            categoryId = (Guid?)null,
            tags = new[] { _tag },
            authorName = "Uzm. Psk. E2E",
            authorTitle = "Klinik Psikolog",
        };

    private async Task<JsonElement> CreateBlogWithCoverAsync()
    {
        var categories = await fixture.Admin.GetFromJsonAsync<JsonElement>("/admin/categories");
        var anxietyId = categories.EnumerateArray()
            .Single(category => category.GetProperty("slug").GetString() == "anksiyete")
            .GetProperty("id")
            .GetGuid();

        var create = await fixture.Admin.PostAsJsonAsync(
            "/admin/blogs",
            new
            {
                title = $"E2E Blog {_tag} Çalışması",
                slug = $"e2e-{_tag}-{_createdBlogIds.Count}",
                excerpt = "E2E testinin ürettiği geçici blog yazısı.",
                content = BlogContent,
                categoryId = anxietyId,
                tags = new[] { _tag },
                authorName = "Uzm. Psk. E2E",
                authorTitle = "Klinik Psikolog",
            });
        create.EnsureSuccessStatusCode();
        var blog = await create.Content.ReadFromJsonAsync<JsonElement>();
        var id = BlogId(blog);
        _createdBlogIds.Add(id);

        Assert.Equal("draft", blog.GetProperty("status").GetString());

        using var form = new MultipartFormDataContent();
        var file = new ByteArrayContent(TinyPng);
        file.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        form.Add(file, "file", "cover.png");
        var cover = await fixture.Admin.PostAsync($"/admin/blogs/{id}/cover", form);
        cover.EnsureSuccessStatusCode();

        var withCover = await cover.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(JsonValueKind.String, withCover.GetProperty("coverImageUrl").ValueKind);
        return withCover;
    }

    private async Task<JsonElement> FetchMobileListAsync(HttpClient client)
        => await client.GetFromJsonAsync<JsonElement>($"/blogs?category={_tag}");

    private async Task<JsonElement> LikeAsync(string slug)
    {
        var response = await fixture.Client.PostAsJsonAsync($"/blogs/{slug}/like", new { });
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }

    private static void AssertZodShape(JsonElement item)
    {
        foreach (var field in (string[])["id", "slug", "title", "excerpt", "coverImage", "publishedAt"])
        {
            Assert.Equal(JsonValueKind.String, item.GetProperty(field).ValueKind);
        }

        Assert.StartsWith("http", item.GetProperty("coverImage").GetString(), StringComparison.Ordinal);
        Assert.EndsWith("Z", item.GetProperty("publishedAt").GetString(), StringComparison.Ordinal);
        Assert.True(item.GetProperty("readingTime").GetInt32() > 0);
        Assert.True(item.GetProperty("likeCount").GetInt32() >= 0);
        Assert.Equal(JsonValueKind.Array, item.GetProperty("categories").ValueKind);
        Assert.Equal(JsonValueKind.String, item.GetProperty("author").GetProperty("name").ValueKind);
        Assert.Equal(JsonValueKind.String, item.GetProperty("author").GetProperty("title").ValueKind);
    }

    private static async Task SetPublishedAtAsync(Guid blogId, DateTimeOffset publishedAt)
    {
        await using var dataSource = new NpgsqlDataSourceBuilder(LocalStack.PostgresConnectionString).Build();
        await using var connection = await dataSource.OpenConnectionAsync(CancellationToken.None);
        await using var command = connection.CreateCommand();
        command.CommandText = "update public.blogs set published_at = @at where id = @id;";
        command.Parameters.AddWithValue("at", publishedAt);
        command.Parameters.AddWithValue("id", blogId);
        await command.ExecuteNonQueryAsync(CancellationToken.None);
    }

    private static async Task<List<string>> ReadAuditActionsAsync(Guid blogId)
    {
        await using var dataSource = new NpgsqlDataSourceBuilder(LocalStack.PostgresConnectionString).Build();
        await using var connection = await dataSource.OpenConnectionAsync(CancellationToken.None);
        await using var command = connection.CreateCommand();
        command.CommandText =
            "select action from public.audit_logs where entity_type = 'blog' and entity_id = @id order by id;";
        command.Parameters.AddWithValue("id", blogId.ToString());

        var actions = new List<string>();
        await using var reader = await command.ExecuteReaderAsync(CancellationToken.None);
        while (await reader.ReadAsync(CancellationToken.None))
        {
            actions.Add(reader.GetString(0));
        }

        return actions;
    }
}
