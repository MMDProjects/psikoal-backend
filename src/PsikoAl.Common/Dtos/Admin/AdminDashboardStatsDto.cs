namespace PsikoAl.Common.Dtos.Admin;

public sealed record AdminDashboardDailyPointDto(
    DateOnly Date,
    int NewListings,
    int NewMatches);

public sealed record AdminDashboardStatsDto(
    int PendingListings,
    int PendingExperts,
    int PendingReviews,
    int TodayNewClients,
    int TodayNewExperts,
    int TodayNewListings,
    int TodayNewOffers,
    int TodayNewMatches,
    int TodayCompletedAssessments,
    int TotalUsers,
    int OpenListings,
    int ActiveMatches,
    IReadOnlyList<AdminDashboardDailyPointDto> LastDays);
