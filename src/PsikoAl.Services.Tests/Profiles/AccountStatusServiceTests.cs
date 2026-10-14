using Microsoft.Extensions.Caching.Memory;
using PsikoAl.Common.Constants;
using PsikoAl.Services.Tests.Support;

namespace PsikoAl.Services.Tests.Profiles;

public sealed class AccountStatusServiceTests
{
    [Theory]
    [InlineData(ProfileStatuses.Active, true)]
    [InlineData(ProfileStatuses.Frozen, false)]
    [InlineData(ProfileStatuses.Deleted, false)]
    public async Task Reflects_profile_status(string status, bool expectedActive)
    {
        using var database = new TestDatabase();
        var profile = await database.AddProfileAsync(status: status);
        using var cache = new MemoryCache(new MemoryCacheOptions());

        var isActive = await new AccountStatusService(database.UnitOfWork, cache)
            .IsActiveAsync(profile.Id, CancellationToken.None);

        Assert.Equal(expectedActive, isActive);
    }

    [Fact]
    public async Task Missing_profile_is_not_blocked()
    {
        using var database = new TestDatabase();
        using var cache = new MemoryCache(new MemoryCacheOptions());

        Assert.True(await new AccountStatusService(database.UnitOfWork, cache)
            .IsActiveAsync(Guid.NewGuid(), CancellationToken.None));
    }

    [Fact]
    public async Task Invalidate_drops_the_cached_status_immediately()
    {
        using var database = new TestDatabase();
        var profile = await database.AddProfileAsync();
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var service = new AccountStatusService(database.UnitOfWork, cache);
        Assert.True(await service.IsActiveAsync(profile.Id, CancellationToken.None));

        profile.Status = ProfileStatuses.Frozen;
        await database.Context.SaveChangesAsync();

        Assert.True(await service.IsActiveAsync(profile.Id, CancellationToken.None));

        service.Invalidate(profile.Id);

        Assert.False(await service.IsActiveAsync(profile.Id, CancellationToken.None));
    }
}
