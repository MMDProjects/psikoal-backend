using PsikoAl.Common.Constants;
using PsikoAl.Common.Dtos.Credit;
using PsikoAl.Common.Presentation;
using PsikoAl.Data.Entities;

namespace PsikoAl.Services.Mapping;

public static class CreditMapper
{
    public static CreditPackageDto ToPackageDto(CreditPackage package)
        => new(
            package.Id,
            package.Name,
            package.Credits,
            package.PriceTry,
            CreditLabelFormatter.Price(package.PriceTry),
            CreditLabelFormatter.UnitPrice(package.PriceTry, package.Credits));

    public static CreditTransactionDto ToTransactionDto(CreditTransactionReadModel row, DateTimeOffset now)
        => new(
            row.Id,
            row.Amount,
            row.BalanceAfter,
            row.Type,
            CreditLabelFormatter.TypeLabel(row.Type),
            DescriptionFor(row),
            row.OfferId,
            row.CreatedAt,
            RelativeTimeTr.From(row.CreatedAt, now));

    // Uzmanın kendi geçmişi: ilan başlığını zaten ilan akışında görüyor. Yönetici
    // düzeltmesinin gerekçesi uzmana açıktır (panel diyaloğu bunu yöneticiye söyler).
    public static string DescriptionFor(CreditTransactionReadModel row)
        => row.Type switch
        {
            CreditTransactionTypes.Purchase => row.PackageName is { Length: > 0 } packageName
                ? $"{packageName} paketi satın alındı"
                : "Kredi paketi satın alındı",
            CreditTransactionTypes.OfferSpend => row.ListingTitle is { Length: > 0 } listingTitle
                ? $"\"{listingTitle}\" ilanına teklif gönderildi"
                : "İlana teklif gönderildi",
            CreditTransactionTypes.AdminAdjust => row.Reason is { Length: > 0 } reason
                ? $"Yönetici düzeltmesi: {reason}"
                : "Yönetici düzeltmesi",
            _ => CreditLabelFormatter.TypeLabel(row.Type),
        };
}
