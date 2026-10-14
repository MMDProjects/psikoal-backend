using PsikoAl.Data.Entities;
using PsikoAl.Services.Mapping;

namespace PsikoAl.Services.Tests.Mapping;

// Sözleşme kilidi: psikoal-app / src/app/profile/personal.tsx -> "İletişim Paylaşımı".
// Ekran kullanıcıya birebir şunu vaat ediyor: "Eşleşme sağlandığında SEÇTİĞİNİZ
// bilgiler uzmanla paylaşılır", altında "Uzman e-posta adresinizi görebilir" ve
// "Uzman telefon numaranızı görebilir" anahtarları.
//
// Bu bayraklar bir dönem yalnız yazılıp geri okunuyordu; hiçbir yerde veriyi
// süzmüyorlardı. Kullanıcı anahtarı kapatıyor, uygulama kabul ediyor, bilgi yine de
// uzmana gidiyordu. Belirtisi yoktu: ne hata, ne uyarı — kullanıcı kendi bilgisinin
// paylaşıldığını hiçbir şekilde göremezdi. KVKK açısından sessiz bir ihlal.
public sealed class MatchMapperPrivacyTests
{
    [Theory]
    [InlineData(true, true)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(false, false)]
    public void ToMatchDto_SharesContactOnlyWhenTheClientAllowedIt(bool shareEmail, bool sharePhone)
    {
        var match = CreateMatch(shareEmail, sharePhone);

        var dto = MatchMapper.ToMatchDto(match);

        Assert.Equal(shareEmail ? "danisan@psikoal.test" : null, dto.Client?.Email);
        Assert.Equal(sharePhone ? "+905551112233" : null, dto.Client?.Phone);
    }

    // Ad ve baş harfler bayraklardan BAĞIMSIZ: eşleşme sonrası tam adın görünmesi
    // ürün kuralı (danışan teklifi kabul ederken bunu açıkça onaylıyor). Süzme
    // yanlışlıkla ada da uygulanırsa uzman kiminle çalıştığını bilemez; bu iddia
    // düzeltmenin fazla geniş uygulanmasını engelliyor.
    [Fact]
    public void ToMatchDto_AlwaysRevealsTheClientFullName()
    {
        var dto = MatchMapper.ToMatchDto(CreateMatch(shareEmail: false, sharePhone: false));

        Assert.Equal("Ayşe Yılmaz", dto.Client?.FullName);
        Assert.Equal("AY", dto.Client?.Initials);
    }

    private static Match CreateMatch(bool shareEmail, bool sharePhone)
    {
        var clientId = Guid.NewGuid();

        return new Match
        {
            Id = Guid.NewGuid(),
            ClientId = clientId,
            ExpertId = Guid.NewGuid(),
            ListingId = Guid.NewGuid(),
            Status = "RELEASED",
            CreatedAt = DateTimeOffset.UtcNow,
            Client = new Profile
            {
                Id = clientId,
                FirstName = "Ayşe",
                LastName = "Yılmaz",
                Email = "danisan@psikoal.test",
                Phone = "+905551112233",
                Role = "client",
                ShareEmail = shareEmail,
                SharePhone = sharePhone,
                CreatedAt = DateTimeOffset.UtcNow,
            },
        };
    }
}
