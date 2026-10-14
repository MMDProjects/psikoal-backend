using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using PsikoAl.Common.Dtos.Blog;
using PsikoAl.Services.Abstractions;

namespace PsikoAl.Api.ContractTests;

// Sözleşme kaynağı: native-atomic/src/domains/blog/schemas/blog.schema.ts ve
// api/useBlogListQuery.ts. Uygulama yanıtı safeParse ile okuyor; tek bir alan
// adı/tipi kayarsa liste BOŞ görünür ve HTTP 200 döndüğü için hiçbir status
// testi bunu yakalamaz. Burada servis sahte, ama JSON'u üreten gerçek pipeline
// (controller, zarf, camelCase, UtcDateTimeOffsetJsonConverter).
public sealed class BlogContractTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private static readonly Regex ZodDateTime = new(
        @"^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}(\.\d+)?Z$",
        RegexOptions.Compiled,
        TimeSpan.FromSeconds(1));

    private static readonly DateTimeOffset PublishedAt = new(2026, 6, 10, 9, 0, 0, TimeSpan.FromHours(3));

    [Fact]
    public async Task Blog_list_matches_BlogListResponseSchema()
    {
        var fake = new FakeBlogService();
        using var client = CreateClient(fake);

        var response = await client.GetAsync("/blogs?category=anksiyete&limit=3", CancellationToken.None);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        Assert.Equal(new BlogListFilters("anksiyete", null, null, 3), fake.LastFilters);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(CancellationToken.None);
        var meta = body.GetProperty("meta");
        foreach (var field in (string[])["page", "total", "perPage"])
        {
            Assert.Equal(JsonValueKind.Number, meta.GetProperty(field).ValueKind);
        }

        var item = Assert.Single(body.GetProperty("data").EnumerateArray());
        AssertBlogListItemShape(item);
        Assert.False(item.TryGetProperty("content", out _), "Liste öğesi content taşımamalı (BlogListItemSchema = omit content).");
    }

    [Fact]
    public async Task Blog_detail_matches_BlogSchema()
    {
        using var client = CreateClient(new FakeBlogService());

        var body = await client.GetFromJsonAsync<JsonElement>("/blogs/anksiyete-ile-basa-cikmak", CancellationToken.None);

        AssertBlogListItemShape(body);
        Assert.Equal(JsonValueKind.String, body.GetProperty("content").ValueKind);
    }

    [Fact]
    public async Task Blog_list_and_detail_do_not_require_authentication()
    {
        using var client = factory.CreateClient();

        var list = await client.GetAsync("/blogs", CancellationToken.None);
        var detail = await client.GetAsync("/blogs/herhangi-bir-yazi", CancellationToken.None);

        Assert.NotEqual(HttpStatusCode.Unauthorized, list.StatusCode);
        Assert.NotEqual(HttpStatusCode.Unauthorized, detail.StatusCode);
    }

    [Fact]
    public async Task Blog_like_requires_authentication()
    {
        using var client = factory.CreateClient();

        var like = await client.PostAsJsonAsync("/blogs/herhangi-bir-yazi/like", new { }, CancellationToken.None);

        Assert.Equal(HttpStatusCode.Unauthorized, like.StatusCode);
    }

    [Fact]
    public async Task Admin_blog_endpoints_require_authentication()
    {
        using var client = factory.CreateClient();
        var id = Guid.NewGuid();

        var responses = new[]
        {
            await client.GetAsync("/admin/blogs", CancellationToken.None),
            await client.GetAsync($"/admin/blogs/{id}", CancellationToken.None),
            await client.PostAsJsonAsync("/admin/blogs", new { title = "t", authorName = "a" }, CancellationToken.None),
            await client.PutAsJsonAsync($"/admin/blogs/{id}", new { title = "t" }, CancellationToken.None),
            await client.DeleteAsync($"/admin/blogs/{id}", CancellationToken.None),
            await client.PostAsJsonAsync($"/admin/blogs/{id}/publish", new { }, CancellationToken.None),
            await client.PostAsJsonAsync($"/admin/blogs/{id}/unpublish", new { }, CancellationToken.None),
            await client.PostAsJsonAsync($"/admin/blogs/{id}/schedule", new { publishAt = DateTimeOffset.UtcNow.AddDays(1) }, CancellationToken.None),
        };

        Assert.All(responses, response => Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode));
    }

    private static void AssertBlogListItemShape(JsonElement item)
    {
        foreach (var field in (string[])["id", "slug", "title", "excerpt", "coverImage", "publishedAt"])
        {
            Assert.Equal(JsonValueKind.String, item.GetProperty(field).ValueKind);
        }

        Assert.True(Uri.TryCreate(item.GetProperty("coverImage").GetString(), UriKind.Absolute, out _), "coverImage z.string().url() olmalı.");
        Assert.Matches(ZodDateTime, item.GetProperty("publishedAt").GetString());
        Assert.Equal("2026-06-10T06:00:00.000000Z", item.GetProperty("publishedAt").GetString());

        var categories = item.GetProperty("categories");
        Assert.Equal(JsonValueKind.Array, categories.ValueKind);
        Assert.All(categories.EnumerateArray(), category => Assert.Equal(JsonValueKind.String, category.ValueKind));

        Assert.True(item.GetProperty("likeCount").GetInt32() >= 0);
        Assert.True(item.GetProperty("readingTime").GetInt32() > 0);
        Assert.Contains(item.GetProperty("liked").ValueKind, new[] { JsonValueKind.True, JsonValueKind.False });

        var author = item.GetProperty("author");
        Assert.Equal(JsonValueKind.String, author.GetProperty("name").ValueKind);
        Assert.Equal(JsonValueKind.String, author.GetProperty("title").ValueKind);
    }

    private HttpClient CreateClient(IBlogService blogService)
        => factory
            .WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
                services.AddScoped(_ => blogService)))
            .CreateClient();

    private sealed class FakeBlogService : IBlogService
    {
        public BlogListFilters? LastFilters { get; private set; }

        public Task<BlogListResult> ListAsync(BlogListFilters filters, Guid? viewerId, CancellationToken cancellationToken)
        {
            LastFilters = filters;
            return Task.FromResult(new BlogListResult([ToListItem(Sample())], filters.EffectivePage, filters.EffectivePerPage, 1));
        }

        public Task<BlogDto> GetAsync(string slugOrId, Guid? viewerId, CancellationToken cancellationToken)
            => Task.FromResult(Sample());

        public Task<BlogLikeResultDto> ToggleLikeAsync(string slugOrId, Guid userId, CancellationToken cancellationToken)
            => Task.FromResult(new BlogLikeResultDto(1, true));

        private static BlogDto Sample()
            => new(
                Guid.NewGuid(),
                "anksiyete-ile-basa-cikmak",
                "Anksiyete ile Başa Çıkmanın 5 Etkili Yolu",
                "Günlük yaşamda anksiyete ile mücadele etmek zor gelebilir.",
                "Birinci paragraf.\n\nİkinci paragraf.",
                "https://localhost.supabase.test/storage/v1/object/public/blog-media/x/cover.jpg",
                ["anksiyete", "teknikler"],
                148,
                false,
                4,
                PublishedAt,
                new BlogAuthorDto("Uzm. Psk. Elif Kaya", "Klinik Psikolog"));

        private static BlogListItemDto ToListItem(BlogDto blog)
            => new(
                blog.Id,
                blog.Slug,
                blog.Title,
                blog.Excerpt,
                blog.CoverImage,
                blog.Categories,
                blog.LikeCount,
                blog.Liked,
                blog.ReadingTime,
                blog.PublishedAt,
                blog.Author);
    }
}
