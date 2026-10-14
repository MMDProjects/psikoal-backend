using PsikoAl.Common.Dtos.Match;
using PsikoAl.Common.Presentation;
using PsikoAl.Data.Entities;

namespace PsikoAl.Services.Mapping;

public static class MatchMapper
{
    public static MatchDto ToMatchDto(Match match)
    {
        var client = match.Client;

        // İletişim paylaşımı danışanın AÇIK tercihine bağlı. Mobil taraftaki
        // "İletişim Paylaşımı" bölümü bunu birebir vaat ediyor:
        // "Eşleşme sağlandığında SEÇTİĞİNİZ bilgiler uzmanla paylaşılır" ve her
        // satır "Uzman e-posta adresinizi / telefon numaranızı görebilir" diyor.
        //
        // Bu bayraklar eskiden yalnız yazılıp geri okunuyordu; hiçbir yerde veriyi
        // süzmüyorlardı. Yani kullanıcı anahtarı kapatıyor, uygulama kabul ediyor
        // ve bilgi yine de paylaşılıyordu — belirtisi olmayan, kullanıcının fark
        // edemeyeceği bir gizlilik ihlali.
        //
        // Ad ve baş harfler BİLEREK süzülmüyor: eşleşme sonrası tam adın görünmesi
        // ürün kuralı (danışan teklifi kabul ederken bunu onaylıyor) ve tersi,
        // uzmanın kiminle çalıştığını bilmemesi anlamına gelirdi.
        var clientDto = new MatchClientDto(
            client?.Id ?? match.ClientId,
            client is null ? "Danışan" : $"{client.FirstName} {client.LastName}",
            client is null ? null : ExpertMapper.InitialsOf(client.FirstName, client.LastName),
            client is { ShareEmail: true } ? client.Email : null,
            client is { SharePhone: true } ? client.Phone : null,
            client?.CreatedAt);

        // Uzmanın iletişimi de aynı kurala bağlı: kendi "İletişim Paylaşımı" tercihi.
        var expertDto = match.Expert is { Profile: { } expertProfile } expert
            ? new MatchExpertDto(
                expert.Id,
                $"{expertProfile.FirstName} {expertProfile.LastName}",
                expert.Title,
                ExpertMapper.InitialsOf(expertProfile.FirstName, expertProfile.LastName),
                expertProfile.ShareEmail ? expertProfile.Email : null,
                expertProfile.SharePhone ? expertProfile.Phone : null)
            : null;

        var listingDto = match.Listing is { } listing
            ? new MatchListingDto(
                listing.Id,
                listing.Title,
                listing.Description,
                listing.Specialization,
                listing.BudgetMin,
                listing.BudgetMax,
                BudgetLabelFormatter.Format(listing.BudgetMin, listing.BudgetMax),
                listing.PreferredSessionType,
                listing.Status,
                listing.City)
            : null;

        var offerDto = match.AcceptedOffer is { } offer
            ? new MatchOfferDto(offer.Id, offer.Title, offer.Price, offer.Description, offer.SessionType, offer.Status)
            : null;

        return new MatchDto(
            match.Id,
            match.ListingId,
            match.AcceptedOfferId,
            match.ClientId,
            match.ExpertId,
            match.Status,
            match.CreatedAt,
            RelativeTimeTr.From(match.CreatedAt),
            clientDto,
            expertDto,
            listingDto,
            offerDto,
            match.ClientReleasedAt,
            match.ExpertReleasedAt);
    }
}
