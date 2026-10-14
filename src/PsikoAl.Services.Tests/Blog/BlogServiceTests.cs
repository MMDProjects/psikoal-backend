using NSubstitute;
using PsikoAl.Common.Constants;
using PsikoAl.Common.Exceptions;
using PsikoAl.Data.Entities;
using PsikoAl.Data.Repositories.Abstractions;
using PsikoAl.Services.Abstractions;
using BlogEntity = PsikoAl.Data.Entities.Blog;

namespace PsikoAl.Services.Tests.Blog;

// Beğeni bir toggle: mock-db/handlers/blog.handlers.js ile aynı davranış.
// Uygulama (useBlogLike) yanıttaki { likeCount, liked } değerini OLDUĞU GİBİ
// gösteriyor — iyimser artırma yapmıyor. Yani sayı ya da bayrak burada
// yanlışsa ekranda doğrudan yanlış görünür.
public sealed class BlogServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 14, 12, 0, 0, TimeSpan.Zero);

    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IBlogRepository _blogs = Substitute.For<IBlogRepository>();
    private readonly IBlogLikeRepository _likes = Substitute.For<IBlogLikeRepository>();
    private readonly IProfileRepository _profiles = Substitute.For<IProfileRepository>();
    private readonly ISupabaseStorageService _storage = Substitute.For<ISupabaseStorageService>();
    private readonly BlogService _service;

    private readonly Guid _userId = Guid.NewGuid();
    private readonly BlogEntity _blog = new()
    {
        Id = Guid.NewGuid(),
        Slug = "anksiyete-ile-basa-cikmak",
        Title = "Anksiyete ile Başa Çıkmanın 5 Etkili Yolu",
        Excerpt = "Özet",
        Content = "Birinci paragraf.\n\nİkinci paragraf.",
        CoverImagePath = "blog-id/cover-1.jpg",
        AuthorName = "Uzm. Psk. Elif Kaya",
        AuthorTitle = "Klinik Psikolog",
        ReadingMinutes = 4,
        Status = BlogStatuses.Published,
        PublishedAt = Now.AddDays(-2),
        Tags = ["anksiyete", "teknikler"],
        Category = new Category
        {
            Slug = "anksiyete",
            Name = "Anksiyete ve Kaygı",
            Icon = "HeartPulse",
            Summary = "s",
            Description = "d",
            BlogTag = "anksiyete",
        },
    };

    public BlogServiceTests()
    {
        _unitOfWork.Blogs.Returns(_blogs);
        _unitOfWork.BlogLikes.Returns(_likes);
        _unitOfWork.Profiles.Returns(_profiles);
        _storage.GetPublicUrl(Arg.Any<string>(), Arg.Any<string>())
            .Returns(call => $"https://cdn.test/storage/v1/object/public/{call.ArgAt<string>(0)}/{call.ArgAt<string>(1)}");
        _blogs.GetPublicAsync(_blog.Slug, Now, Arg.Any<CancellationToken>()).Returns(_blog);
        _profiles.ExistsAsync(_userId, Arg.Any<CancellationToken>()).Returns(true);

        _service = new BlogService(_unitOfWork, _storage, new FixedTimeProvider(Now));
    }

    [Fact]
    public async Task ToggleLike_LikesWhenTheUserHasNotLikedYet()
    {
        _likes.FindAsync(_blog.Id, _userId, Arg.Any<CancellationToken>()).Returns((BlogLike?)null);
        _likes.CountForBlogAsync(_blog.Id, Arg.Any<CancellationToken>()).Returns(149);

        var result = await _service.ToggleLikeAsync(_blog.Slug, _userId, CancellationToken.None);

        Assert.True(result.Liked);
        Assert.Equal(149, result.LikeCount);
        await _likes.Received(1).AddAsync(
            Arg.Is<BlogLike>(like => like.BlogId == _blog.Id && like.UserId == _userId),
            Arg.Any<CancellationToken>());
        _likes.DidNotReceive().Delete(Arg.Any<BlogLike>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ToggleLike_UnlikesWhenTheUserAlreadyLiked()
    {
        var existing = new BlogLike { BlogId = _blog.Id, UserId = _userId };
        _likes.FindAsync(_blog.Id, _userId, Arg.Any<CancellationToken>()).Returns(existing);
        _likes.CountForBlogAsync(_blog.Id, Arg.Any<CancellationToken>()).Returns(147);

        var result = await _service.ToggleLikeAsync(_blog.Slug, _userId, CancellationToken.None);

        Assert.False(result.Liked);
        Assert.Equal(147, result.LikeCount);
        _likes.Received(1).Delete(existing);
        await _likes.DidNotReceive().AddAsync(Arg.Any<BlogLike>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ToggleLike_OnAHiddenOrUnknownPostIsBlogNotFound()
    {
        var error = await Assert.ThrowsAsync<DomainException>(
            () => _service.ToggleLikeAsync("taslak-yazi", _userId, CancellationToken.None));

        Assert.Equal(ErrorKeys.BlogNotFound, error.ErrorKey);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ToggleLike_RequiresAProfile()
    {
        var stranger = Guid.NewGuid();

        var error = await Assert.ThrowsAsync<DomainException>(
            () => _service.ToggleLikeAsync(_blog.Slug, stranger, CancellationToken.None));

        Assert.Equal(ErrorKeys.ProfileNotFound, error.ErrorKey);
    }

    [Fact]
    public async Task Get_ReturnsTheShapeTheAppRenders()
    {
        _likes.CountForBlogAsync(_blog.Id, Arg.Any<CancellationToken>()).Returns(12);

        var blog = await _service.GetAsync(_blog.Slug, viewerId: null, CancellationToken.None);

        Assert.Equal(_blog.Id, blog.Id);
        Assert.Equal(_blog.Content, blog.Content);
        Assert.Equal("https://cdn.test/storage/v1/object/public/blog-media/blog-id/cover-1.jpg", blog.CoverImage);
        Assert.Equal(["anksiyete", "teknikler"], blog.Categories);
        Assert.Equal(12, blog.LikeCount);
        Assert.False(blog.Liked);
        Assert.Equal(4, blog.ReadingTime);
        Assert.Equal(_blog.PublishedAt, blog.PublishedAt);
        Assert.Equal("Uzm. Psk. Elif Kaya", blog.Author.Name);
        Assert.Equal("Klinik Psikolog", blog.Author.Title);
        await _likes.DidNotReceive().FindAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Get_MarksLikedForTheViewerWhoLikedIt()
    {
        _likes.FindAsync(_blog.Id, _userId, Arg.Any<CancellationToken>())
            .Returns(new BlogLike { BlogId = _blog.Id, UserId = _userId });

        var blog = await _service.GetAsync(_blog.Slug, _userId, CancellationToken.None);

        Assert.True(blog.Liked);
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
