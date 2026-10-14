using PsikoAl.Common.Constants;
using PsikoAl.Common.Exceptions;
using PsikoAl.Data.Entities;
using PsikoAl.Data.Repositories;
using BlogEntity = PsikoAl.Data.Entities.Blog;

namespace PsikoAl.Services.Tests.Blog;

// Görünürlük kuralı tek yerde (BlogVisibility.IsPublicAt) ve hem liste hem detay
// hem beğeni ucu aynı ifadeyi kullanıyor. Bu kural kayarsa taslak ya da saati
// gelmemiş plânlı yazı uygulamaya sızar — HTTP 200 döner, hiçbir status testi
// fark etmez.
public sealed class BlogVisibilityTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 14, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void IsPublicAt_ShowsPublishedAndDueScheduledPostsOnly()
    {
        var blogs = new[]
        {
            Make("yayinda", BlogStatuses.Published, Now.AddDays(-1)),
            Make("taslak", BlogStatuses.Draft, null),
            Make("plan-gecmis", BlogStatuses.Scheduled, Now.AddMinutes(-1)),
            Make("plan-tam-simdi", BlogStatuses.Scheduled, Now),
            Make("plan-gelecek", BlogStatuses.Scheduled, Now.AddMinutes(1)),
            Make("kapaksiz", BlogStatuses.Published, Now.AddDays(-1), cover: null),
        };

        var visible = blogs.AsQueryable().Where(BlogVisibility.IsPublicAt(Now)).Select(blog => blog.Slug).ToList();

        Assert.Equal(["yayinda", "plan-gecmis", "plan-tam-simdi"], visible);
    }

    [Fact]
    public void IsLive_AgreesWithTheQueryExpression()
    {
        var blogs = new[]
        {
            Make("a", BlogStatuses.Published, Now.AddDays(-1)),
            Make("b", BlogStatuses.Draft, null),
            Make("c", BlogStatuses.Scheduled, Now.AddHours(-1)),
            Make("d", BlogStatuses.Scheduled, Now.AddHours(1)),
            Make("e", BlogStatuses.Published, Now, cover: null),
        };

        var predicate = BlogVisibility.IsPublicAt(Now).Compile();
        foreach (var blog in blogs)
        {
            Assert.Equal(
                predicate(blog),
                BlogVisibility.IsLive(blog.Status, blog.PublishedAt, blog.CoverImagePath is not null, Now));
        }
    }

    [Fact]
    public void WithTag_MatchesEitherTheCategoryBlogTagOrAFreeTag()
    {
        var anxiety = new Category
        {
            Slug = "anksiyete",
            Name = "Anksiyete ve Kaygı",
            Icon = "HeartPulse",
            Summary = "s",
            Description = "d",
            BlogTag = "anksiyete",
        };

        var blogs = new[]
        {
            Make("kategoriden", BlogStatuses.Published, Now, category: anxiety),
            Make("etiketten", BlogStatuses.Published, Now, tags: ["anksiyete", "teknikler"]),
            Make("alakasiz", BlogStatuses.Published, Now, tags: ["uyku"]),
        };

        var matched = blogs.AsQueryable().WithTag("anksiyete").Select(blog => blog.Slug).ToList();
        var unfiltered = blogs.AsQueryable().WithTag(" ").Count();

        Assert.Equal(["kategoriden", "etiketten"], matched);
        Assert.Equal(3, unfiltered);
    }

    [Fact]
    public void Publish_RequiresCoverExcerptAndContent()
    {
        var withoutCover = Make("kapaksiz", BlogStatuses.Draft, null, cover: null);
        var withoutExcerpt = Make("ozetsiz", BlogStatuses.Draft, null);
        withoutExcerpt.Excerpt = " ";

        var coverError = Assert.Throws<DomainException>(() => BlogPublishRules.Publish(withoutCover, Now));
        var excerptError = Assert.Throws<DomainException>(() => BlogPublishRules.Publish(withoutExcerpt, Now));

        Assert.Equal(ErrorKeys.BlogNotPublishable, coverError.ErrorKey);
        Assert.Equal("coverImage", coverError.Field);
        Assert.Equal("excerpt", excerptError.Field);
        Assert.Equal(BlogStatuses.Draft, withoutCover.Status);
    }

    [Fact]
    public void Publish_StampsNowForADraftButKeepsTheOriginalDateOfALivePost()
    {
        var draft = Make("taslak", BlogStatuses.Draft, null);
        var live = Make("canli", BlogStatuses.Published, Now.AddDays(-30));

        BlogPublishRules.Publish(draft, Now);
        BlogPublishRules.Publish(live, Now);

        Assert.Equal(BlogStatuses.Published, draft.Status);
        Assert.Equal(Now, draft.PublishedAt);
        Assert.Equal(Now.AddDays(-30), live.PublishedAt);
    }

    [Fact]
    public void Publish_OnAFutureScheduledPostPublishesItNow()
    {
        var scheduled = Make("plan", BlogStatuses.Scheduled, Now.AddDays(3));

        BlogPublishRules.Publish(scheduled, Now);

        Assert.Equal(BlogStatuses.Published, scheduled.Status);
        Assert.Equal(Now, scheduled.PublishedAt);
    }

    [Fact]
    public void Schedule_RejectsPastTimesAndStoresUtc()
    {
        var blog = Make("plan", BlogStatuses.Draft, null);

        var error = Assert.Throws<DomainException>(() => BlogPublishRules.Schedule(blog, Now.AddMinutes(-5), Now));
        Assert.Equal(ErrorKeys.BlogScheduleMustBeFuture, error.ErrorKey);
        Assert.Equal(BlogStatuses.Draft, blog.Status);

        var istanbulMorning = new DateTimeOffset(2026, 10, 20, 9, 0, 0, TimeSpan.FromHours(3));
        BlogPublishRules.Schedule(blog, istanbulMorning, Now);

        Assert.Equal(BlogStatuses.Scheduled, blog.Status);
        Assert.Equal(TimeSpan.Zero, blog.PublishedAt?.Offset);
        Assert.Equal(istanbulMorning, blog.PublishedAt);
    }

    [Fact]
    public void Unpublish_ReturnsToDraftAndClearsTheDate()
    {
        var blog = Make("canli", BlogStatuses.Published, Now);

        BlogPublishRules.Unpublish(blog);

        Assert.Equal(BlogStatuses.Draft, blog.Status);
        Assert.Null(blog.PublishedAt);
    }

    private static BlogEntity Make(
        string slug,
        string status,
        DateTimeOffset? publishedAt,
        string? cover = "id/cover.jpg",
        Category? category = null,
        List<string>? tags = null)
        => new()
        {
            Slug = slug,
            Title = slug,
            Excerpt = "Özet",
            Content = "İçerik",
            AuthorName = "Uzm. Psk. Test",
            Status = status,
            PublishedAt = publishedAt,
            CoverImagePath = cover,
            Category = category,
            Tags = tags ?? [],
        };
}
