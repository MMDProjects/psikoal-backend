using NSubstitute;
using PsikoAl.Common.Constants;
using PsikoAl.Common.Exceptions;
using PsikoAl.Data.Repositories.Abstractions;
using PsikoAl.Services.Abstractions;
using PsikoAl.Services.Tests.Admin;
using PsikoAl.Services.Tests.Support;
using BlogEntity = PsikoAl.Data.Entities.Blog;

namespace PsikoAl.Services.Tests.Blog;

// Kapak her yüklemede yeni bir adla saklanıyor (CDN önbelleği eski görseli
// göstermesin diye). Eskisi silinmezse blog-media bucket'ı her değişiklikte
// sahipsiz görsellerle büyür; yazı silinince de klasörü kalırdı.
public sealed class AdminBlogStorageCleanupTests
{
    private readonly AdminTestDoubles _doubles = new(AdminRoles.ContentEditor);
    private readonly IBlogRepository _blogs = Substitute.For<IBlogRepository>();
    private readonly IBlogLikeRepository _likes = Substitute.For<IBlogLikeRepository>();
    private readonly IUploadService _upload = Substitute.For<IUploadService>();
    private readonly FakeStorageService _storage = new();
    private readonly AdminBlogService _service;

    private readonly BlogEntity _blog = new()
    {
        Id = Guid.NewGuid(),
        Slug = "uyku-hijyeni",
        Title = "Uyku Hijyeni",
        AuthorName = "Uzm. Psk. Test",
        CoverImagePath = "blog-id/cover-eski.jpg",
    };

    public AdminBlogStorageCleanupTests()
    {
        _doubles.UnitOfWork.Blogs.Returns(_blogs);
        _doubles.UnitOfWork.BlogLikes.Returns(_likes);
        _blogs.GetByIdAsync(_blog.Id, Arg.Any<CancellationToken>()).Returns(_blog);
        _upload.UploadBlogCoverAsync(_blog.Id, Arg.Any<Stream>(), "image/png", Arg.Any<CancellationToken>())
            .Returns("blog-id/cover-yeni.png");

        _service = new AdminBlogService(_doubles.UnitOfWork, _doubles.AdminGuard, _upload, _storage, TimeProvider.System);
    }

    [Fact]
    public async Task SetCover_DeletesThePreviousCoverAfterSaving()
    {
        using var content = new MemoryStream([0x89, 0x50, 0x4E, 0x47]);

        var detail = await _service.SetCoverAsync(_doubles.Admin.AuthUserId, _blog.Id, content, "image/png", CancellationToken.None);

        Assert.Equal("https://public.test/blog-media/blog-id/cover-yeni.png", detail.CoverImageUrl);
        Assert.Equal([(StorageBuckets.BlogMedia, "blog-id/cover-eski.jpg")], _storage.DeletedObjects);
        Assert.Contains(_doubles.AddedAuditLogs, log => log.Action == "admin.blog_cover");
    }

    [Fact]
    public async Task SetCover_StillSucceedsWhenStorageCleanupFails()
    {
        _storage.DeleteFailure = new DomainException(ErrorKeys.StorageUploadFailed);
        using var content = new MemoryStream([0x89, 0x50, 0x4E, 0x47]);

        var detail = await _service.SetCoverAsync(_doubles.Admin.AuthUserId, _blog.Id, content, "image/png", CancellationToken.None);

        Assert.Equal("blog-id/cover-yeni.png", _blog.CoverImagePath);
        Assert.NotNull(detail.CoverImageUrl);
    }

    [Fact]
    public async Task Delete_RemovesTheBlogsMediaFolder()
    {
        await _service.DeleteAsync(_doubles.Admin.AuthUserId, _blog.Id, CancellationToken.None);

        _blogs.Received(1).Delete(_blog);
        Assert.Equal([(StorageBuckets.BlogMedia, _blog.Id.ToString())], _storage.DeletedFolders);
        Assert.Contains(_doubles.AddedAuditLogs, log => log.Action == "admin.blog_delete");
    }
}
