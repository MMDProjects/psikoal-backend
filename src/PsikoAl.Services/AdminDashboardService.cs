using Microsoft.EntityFrameworkCore;
using PsikoAl.Common.Constants;
using PsikoAl.Common.Dtos.Admin;
using PsikoAl.Data.Repositories.Abstractions;
using PsikoAl.Services.Abstractions;

namespace PsikoAl.Services;

public sealed class AdminDashboardService(IUnitOfWork unitOfWork) : IAdminDashboardService
{
    private const int TrendDayCount = 14;

    public async Task<AdminDashboardStatsDto> GetStatsAsync(CancellationToken cancellationToken)
    {
        var todayStart = new DateTimeOffset(DateTime.UtcNow.Date, TimeSpan.Zero);
        var trendStart = todayStart.AddDays(-(TrendDayCount - 1));

        var pendingListings = await unitOfWork.Listings.Query()
            .CountAsync(listing => listing.Status == ListingStatuses.PendingApproval, cancellationToken);

        var pendingExperts = await unitOfWork.Experts.Query()
            .CountAsync(expert => expert.Status == ExpertStatuses.Pending, cancellationToken);

        var pendingReviews = await unitOfWork.Reviews.Query()
            .CountAsync(review => review.Status == ReviewStatuses.Pending, cancellationToken);

        var todayNewClients = await unitOfWork.Profiles.Query()
            .CountAsync(profile => profile.Role == ProfileRoles.Client && profile.CreatedAt >= todayStart, cancellationToken);

        var todayNewExperts = await unitOfWork.Profiles.Query()
            .CountAsync(profile => profile.Role == ProfileRoles.Expert && profile.CreatedAt >= todayStart, cancellationToken);

        var todayNewListings = await unitOfWork.Listings.Query()
            .CountAsync(listing => listing.CreatedAt >= todayStart, cancellationToken);

        var todayNewOffers = await unitOfWork.Offers.Query()
            .CountAsync(offer => offer.CreatedAt >= todayStart, cancellationToken);

        var todayNewMatches = await unitOfWork.Matches.Query()
            .CountAsync(match => match.CreatedAt >= todayStart, cancellationToken);

        var todayCompletedAssessments = await unitOfWork.AssessmentResults.Query()
            .CountAsync(result => result.CreatedAt >= todayStart, cancellationToken);

        var totalUsers = await unitOfWork.Profiles.Query()
            .CountAsync(profile => profile.Status != ProfileStatuses.Deleted, cancellationToken);

        var openListings = await unitOfWork.Listings.Query()
            .CountAsync(listing => listing.Status == ListingStatuses.Open, cancellationToken);

        var activeMatches = await unitOfWork.Matches.Query()
            .CountAsync(match => match.Status == MatchStatuses.Active, cancellationToken);

        // Gruplama BELLEKTE yapılıyor, veritabanında DEĞİL.
        //
        // `GroupBy(x => x.CreatedAt.UtcDateTime.Date)` Npgsql tarafından SQL'e
        // ÇEVRİLEMİYOR: timestamptz kolonu için DateTimeOffset -> DateTime -> Date
        // zinciri "No coercion operator is defined between types 'DateTimeOffset'
        // and 'Nullable<DateTime>'" ile patlıyordu. Sonuç: /admin/dashboard/stats
        // 500 dönüyor ve panelin AÇILIŞ SAYFASI tamamen boş kalıyordu — hem de
        // sessizce, çünkü Blazor sayfası hatayı yakalayıp boş tablo gösteriyor.
        //
        // Yalnız CreatedAt kolonu ve yalnız trend penceresi çekiliyor; taşınan veri
        // gün sayısıyla sınırlı, satırın tamamı hiç okunmuyor.
        var listingDates = await unitOfWork.Listings.Query()
            .Where(listing => listing.CreatedAt >= trendStart)
            .Select(listing => listing.CreatedAt)
            .ToListAsync(cancellationToken);

        var matchDates = await unitOfWork.Matches.Query()
            .Where(match => match.CreatedAt >= trendStart)
            .Select(match => match.CreatedAt)
            .ToListAsync(cancellationToken);

        var listingCounts = listingDates
            .GroupBy(createdAt => DateOnly.FromDateTime(createdAt.UtcDateTime))
            .ToDictionary(group => group.Key, group => group.Count());

        var matchCounts = matchDates
            .GroupBy(createdAt => DateOnly.FromDateTime(createdAt.UtcDateTime))
            .ToDictionary(group => group.Key, group => group.Count());

        var trendStartDay = DateOnly.FromDateTime(trendStart.UtcDateTime);
        var lastDays = Enumerable.Range(0, TrendDayCount)
            .Select(offset => trendStartDay.AddDays(offset))
            .Select(day => new AdminDashboardDailyPointDto(
                day,
                listingCounts.GetValueOrDefault(day),
                matchCounts.GetValueOrDefault(day)))
            .ToList();

        return new AdminDashboardStatsDto(
            pendingListings,
            pendingExperts,
            pendingReviews,
            todayNewClients,
            todayNewExperts,
            todayNewListings,
            todayNewOffers,
            todayNewMatches,
            todayCompletedAssessments,
            totalUsers,
            openListings,
            activeMatches,
            lastDays);
    }
}
