using PsikoAl.Common.Presentation;

namespace PsikoAl.Services.Tests.Presentation;

// Sözleşme kilidi: psikoal-app / mock-db/helpers.js -> relativeTimeTR.
// Frontend bu metni hesaplamaz, olduğu gibi basar (CLAUDE.md backend sözleşmesi).
public sealed class RelativeTimeTrTests
{
    private static readonly DateTimeOffset Now = new(2026, 6, 15, 12, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData(0, "şimdi")]
    [InlineData(59, "şimdi")]
    [InlineData(60, "1 dakika önce")]
    [InlineData(60 * 59, "59 dakika önce")]
    [InlineData(60 * 60, "1 saat önce")]
    [InlineData(60 * 60 * 23, "23 saat önce")]
    [InlineData(60 * 60 * 24, "dün")]
    [InlineData(60 * 60 * 24 * 3, "3 gün önce")]
    [InlineData(60 * 60 * 24 * 7, "1 hafta önce")]
    [InlineData(60 * 60 * 24 * 60, "2 ay önce")]
    [InlineData(60 * 60 * 24 * 400, "1 yıl önce")]
    public void From_ProducesTheTurkishRelativeLabel(int secondsAgo, string expected)
        => Assert.Equal(expected, RelativeTimeTr.From(Now.AddSeconds(-secondsAgo), Now));
}
