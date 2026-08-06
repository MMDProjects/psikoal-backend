namespace PsikoAl.Common.Dtos.Offer;

public sealed record OfferListResult(IReadOnlyList<OfferDto> Data, int Total, int? PendingCount);
