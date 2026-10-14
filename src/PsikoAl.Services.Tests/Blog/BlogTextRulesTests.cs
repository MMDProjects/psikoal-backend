using PsikoAl.Common.Presentation;
using PsikoAl.Common.Text;
using PsikoAl.Services.Abstractions;

namespace PsikoAl.Services.Tests.Blog;

// Kısa ad mobilde /blog/{slug} adresinin kendisi. Türkçe karakter yanlış
// katlanırsa ("ı" düşer, "ş" kalır) adres ya çirkinleşir ya da DB'deki
// check (^[a-z0-9]+(-[a-z0-9]+)*$) kaydı reddeder.
public sealed class BlogTextRulesTests
{
    [Theory]
    [InlineData("Anksiyete ile Başa Çıkmanın 5 Etkili Yolu", "anksiyete-ile-basa-cikmanin-5-etkili-yolu")]
    [InlineData("İlk Terapi Seansında Neler Olur?", "ilk-terapi-seansinda-neler-olur")]
    [InlineData("Uyku ve Ruh Sağlığı: Neden Bu Kadar Önemli?", "uyku-ve-ruh-sagligi-neden-bu-kadar-onemli")]
    [InlineData("  ÖFKE   YÖNETİMİ — Iğdır  ", "ofke-yonetimi-igdir")]
    [InlineData("Sosyal Medya & Dijital Denge", "sosyal-medya-dijital-denge")]
    [InlineData("Café résumé", "cafe-resume")]
    public void FromTitle_FoldsTurkishCharactersAndCollapsesSeparators(string title, string expected)
    {
        var slug = SlugGenerator.FromTitle(title);

        Assert.Equal(expected, slug);
        Assert.True(SlugGenerator.IsValid(slug));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("?!—")]
    public void FromTitle_ReturnsEmptyWhenNothingUsableRemains(string? title)
        => Assert.Equal(string.Empty, SlugGenerator.FromTitle(title));

    [Fact]
    public void FromTitle_TruncatesWithoutLeavingATrailingSeparator()
    {
        var slug = SlugGenerator.FromTitle(string.Join(' ', Enumerable.Repeat("terapi", 30)));

        Assert.True(slug.Length <= SlugGenerator.MaxLength);
        Assert.False(slug.EndsWith('-'));
        Assert.True(SlugGenerator.IsValid(slug));
    }

    [Theory]
    [InlineData("anksiyete-ile-basa-cikmak", true)]
    [InlineData("Anksiyete", false)]
    [InlineData("çift-terapisi", false)]
    [InlineData("-bas", false)]
    [InlineData("son-", false)]
    [InlineData("cift--tire", false)]
    [InlineData("", false)]
    public void IsValid_MatchesTheDatabaseCheck(string slug, bool expected)
        => Assert.Equal(expected, SlugGenerator.IsValid(slug));

    [Fact]
    public void TagNormalizer_StripsHashLowercasesTurkishAndDeduplicates()
    {
        var tags = BlogTagNormalizer.Normalize(["#Kendine Bakım", "kendinebakım", " ", "İLİŞKİ", "#ılık"]);

        Assert.Equal(["kendinebakım", "ilişki", "ılık"], tags);
    }

    [Fact]
    public void TagNormalizer_CapsTheNumberOfTags()
    {
        var tags = BlogTagNormalizer.Normalize(Enumerable.Range(0, 20).Select(index => $"etiket{index}"));

        Assert.Equal(BlogTagNormalizer.MaxTagCount, tags.Count);
    }

    [Theory]
    [InlineData(null, 1)]
    [InlineData("", 1)]
    [InlineData("tek kelime", 1)]
    [InlineData(200, 1)]
    [InlineData(201, 2)]
    [InlineData(1000, 5)]
    public void ReadingTime_IsCeilingOfWordsOverTwoHundredAndNeverZero(object? input, int expectedMinutes)
    {
        var content = input is int words ? string.Join(' ', Enumerable.Repeat("kelime", words)) : (string?)input;

        Assert.Equal(expectedMinutes, ReadingTimeCalculator.MinutesFor(content));
    }

    [Fact]
    public void ListFilters_LimitWinsOverPagingAndForcesFirstPage()
    {
        var filters = new BlogListFilters(Category: null, Page: 4, PerPage: 10, Limit: 3);

        Assert.Equal(1, filters.EffectivePage);
        Assert.Equal(3, filters.EffectivePerPage);
    }

    [Fact]
    public void ListFilters_DefaultsMatchTheMockAndClampExtremes()
    {
        Assert.Equal(20, new BlogListFilters(null, null, null, null).EffectivePerPage);
        Assert.Equal(1, new BlogListFilters(null, -5, null, null).EffectivePage);
        Assert.Equal(BlogListFilters.MaxPerPage, new BlogListFilters(null, null, 1000, null).EffectivePerPage);
        Assert.Equal(1, new BlogListFilters(null, null, null, 0).EffectivePerPage);
    }
}
