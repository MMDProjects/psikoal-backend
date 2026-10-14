using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using PsikoAl.Common.Constants;
using PsikoAl.Data.Entities;
using PsikoAl.Services.Tests.Support;

namespace PsikoAl.Services.Tests.Profiles;

// DELETE /auth/me eskiden yalnız profil adını/e-postasını değiştiriyordu; test sonuçları,
// ilan metinleri, bildirimler, push token'ları, Storage dosyaları ve GoTrue e-postası
// kalıyordu. KVKK m.7 silme talebi bunların hepsini kapsar.
public sealed class AccountErasureServiceTests
{
    [Fact]
    public async Task Client_erasure_removes_personal_data_and_closes_open_work()
    {
        using var database = new TestDatabase();
        var client = await database.AddProfileAsync();
        var expert = await database.AddProfileAsync(role: ProfileRoles.Expert);
        var result = await database.AddAssessmentResultAsync(client.Id);
        var openListing = AddListing(database, client.Id, ListingStatuses.Open, result.Id);
        var matchedListing = AddListing(database, client.Id, ListingStatuses.Matched, assessmentResultId: null);
        var pendingOffer = AddOffer(database, openListing.Id, expert.Id, OfferStatuses.Pending);
        var match = AddMatch(database, matchedListing.Id, client.Id, expert.Id);
        database.Context.Reviews.Add(new Review
        {
            Id = Guid.NewGuid(), ExpertId = expert.Id, ClientId = client.Id, MatchId = match.Id, Rating = 5,
            Comment = "Kaygı bozukluğum için çok yardımcı oldu.",
        });
        database.Context.Notifications.Add(new Notification
        {
            Id = Guid.NewGuid(), UserId = client.Id, Type = NotificationTypes.OfferReceived, Title = "t", Body = "Uzman X teklif verdi",
        });
        database.Context.PushTokens.Add(new PushToken
        {
            Id = Guid.NewGuid(), UserId = client.Id, Token = "ExponentPushToken[x]", Platform = "ios",
        });
        await database.Context.SaveChangesAsync();
        var (service, admin, storage) = CreateService(database);

        await service.EraseAsync(client.Id, CancellationToken.None);
        database.Context.ChangeTracker.Clear();

        var profile = Assert.Single(database.Context.Profiles, candidate => candidate.Id == client.Id);
        Assert.Equal(ProfileStatuses.Deleted, profile.Status);
        Assert.Equal(AccountErasureService.AnonymizedEmail(client.Id), profile.Email);
        Assert.Null(profile.Phone);
        Assert.Equal(AccountErasureService.AnonymizedEmail(client.Id), admin.AnonymizedUsers[client.Id]);

        Assert.Empty(database.Context.AssessmentResults);
        Assert.Empty(database.Context.Notifications);
        Assert.Empty(database.Context.PushTokens);

        var listings = database.Context.Listings.ToList();
        Assert.All(listings, listing =>
        {
            Assert.Equal(AccountErasureService.DeletedListingTitle, listing.Title);
            Assert.Equal(string.Empty, listing.Description);
            Assert.Null(listing.AssessmentResultId);
        });
        Assert.Equal(ListingStatuses.Closed, listings.Single(listing => listing.Id == openListing.Id).Status);
        Assert.Equal(ListingStatuses.Matched, listings.Single(listing => listing.Id == matchedListing.Id).Status);

        Assert.Equal(OfferStatuses.Rejected, database.Context.Offers.Single(offer => offer.Id == pendingOffer.Id).Status);
        Assert.Equal(MatchStatuses.Released, database.Context.Matches.Single().Status);
        Assert.Equal(string.Empty, database.Context.Reviews.Single().Comment);

        Assert.Contains(("avatars", client.Id.ToString()), storage.DeletedFolders);
        Assert.Contains(("documents", client.Id.ToString()), storage.DeletedFolders);

        var audit = Assert.Single(database.Context.AuditLogs);
        Assert.Equal("user.delete_self", audit.Action);
    }

    [Fact]
    public async Task Expert_erasure_withdraws_offers_and_clears_profile_documents()
    {
        using var database = new TestDatabase();
        var expertProfile = await database.AddProfileAsync(role: ProfileRoles.Expert);
        var client = await database.AddProfileAsync();
        database.Context.Experts.Add(new Expert
        {
            Id = expertProfile.Id, Title = "Klinik Psikolog", Bio = "Uzun biyografi", Education = "Hacettepe",
            CvUrl = $"{expertProfile.Id}/cv.pdf", Certificates = [$"{expertProfile.Id}/certificates/a.pdf"],
            Status = ExpertStatuses.Approved,
        });
        var listing = AddListing(database, client.Id, ListingStatuses.Open, assessmentResultId: null);
        var offer = AddOffer(database, listing.Id, expertProfile.Id, OfferStatuses.Pending);
        await database.Context.SaveChangesAsync();
        var (service, _, _) = CreateService(database);

        await service.EraseAsync(expertProfile.Id, CancellationToken.None);
        database.Context.ChangeTracker.Clear();

        var expert = database.Context.Experts.Single();
        Assert.Equal(string.Empty, expert.Bio);
        Assert.Null(expert.CvUrl);
        Assert.Empty(expert.Certificates);
        Assert.Null(expert.Education);
        Assert.Equal(ExpertStatuses.Rejected, expert.Status);
        Assert.Equal(OfferStatuses.Withdrawn, database.Context.Offers.Single(candidate => candidate.Id == offer.Id).Status);
        Assert.Equal(ListingStatuses.Open, database.Context.Listings.Single().Status);
    }

    [Fact]
    public async Task Erasure_is_idempotent_and_audits_only_once()
    {
        using var database = new TestDatabase();
        var client = await database.AddProfileAsync();
        var (service, _, _) = CreateService(database);

        await service.EraseAsync(client.Id, CancellationToken.None);
        await service.EraseAsync(client.Id, CancellationToken.None);

        Assert.Single(database.Context.AuditLogs);
        Assert.Equal(ProfileStatuses.Deleted, database.Context.Profiles.Single().Status);
    }

    [Fact]
    public async Task Auth_provider_failure_leaves_database_untouched_so_the_user_can_retry()
    {
        using var database = new TestDatabase();
        var client = await database.AddProfileAsync();
        await database.AddAssessmentResultAsync(client.Id);
        var (service, admin, _) = CreateService(database);
        admin.AnonymizeFailure = new HttpRequestException("GoTrue kapalı");

        await Assert.ThrowsAsync<HttpRequestException>(() => service.EraseAsync(client.Id, CancellationToken.None));
        database.Context.ChangeTracker.Clear();

        Assert.Equal(ProfileStatuses.Active, database.Context.Profiles.Single().Status);
        Assert.Single(database.Context.AssessmentResults);
    }

    [Fact]
    public async Task Storage_failure_is_logged_but_does_not_undo_the_erasure()
    {
        using var database = new TestDatabase();
        var client = await database.AddProfileAsync();
        var (service, _, storage) = CreateService(database);
        storage.DeleteFailure = new HttpRequestException("Storage kapalı");

        await service.EraseAsync(client.Id, CancellationToken.None);

        Assert.Equal(ProfileStatuses.Deleted, database.Context.Profiles.Single().Status);
    }

    private static (AccountErasureService Service, FakeSupabaseAdminService Admin, FakeStorageService Storage) CreateService(
        TestDatabase database)
    {
        var admin = new FakeSupabaseAdminService();
        var storage = new FakeStorageService();
        var accountStatus = new AccountStatusService(database.UnitOfWork, new MemoryCache(new MemoryCacheOptions()));
        var service = new AccountErasureService(
            database.UnitOfWork,
            admin,
            storage,
            accountStatus,
            NullLogger<AccountErasureService>.Instance);
        return (service, admin, storage);
    }

    private static Listing AddListing(TestDatabase database, Guid clientId, string status, Guid? assessmentResultId)
    {
        var listing = new Listing
        {
            Id = Guid.NewGuid(),
            ClientId = clientId,
            Title = "Kaygı ve panik atak için destek",
            Description = "Geceleri uyuyamıyorum.",
            PreferredSessionType = "online",
            City = "Ankara",
            BudgetMin = 500,
            BudgetMax = 900,
            Status = status,
            AssessmentResultId = assessmentResultId,
        };
        database.Context.Listings.Add(listing);
        return listing;
    }

    private static Offer AddOffer(TestDatabase database, Guid listingId, Guid expertId, string status)
    {
        var offer = new Offer
        {
            Id = Guid.NewGuid(), ListingId = listingId, ExpertId = expertId, Price = 700, SessionType = "online", Status = status,
        };
        database.Context.Offers.Add(offer);
        return offer;
    }

    private static Match AddMatch(TestDatabase database, Guid listingId, Guid clientId, Guid expertId)
    {
        var match = new Match
        {
            Id = Guid.NewGuid(), ListingId = listingId, AcceptedOfferId = Guid.NewGuid(), ClientId = clientId, ExpertId = expertId,
        };
        database.Context.Matches.Add(match);
        return match;
    }
}
