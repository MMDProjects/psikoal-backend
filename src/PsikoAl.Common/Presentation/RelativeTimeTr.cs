namespace PsikoAl.Common.Presentation;

// Davranış referansı: psikoal-app reposu, mock-db/helpers.js -> relativeTimeTR (birebir eşleşir).
// Sözleşme, bu dosyanın unit testlerinde kilitlenir (PsikoAl.Services.Tests/Presentation).
public static class RelativeTimeTr
{
    public static string From(DateTimeOffset value, DateTimeOffset? now = null)
    {
        var reference = now ?? DateTimeOffset.UtcNow;
        var diff = reference - value;

        var seconds = (int)diff.TotalSeconds;
        var minutes = seconds / 60;
        var hours = minutes / 60;
        var days = hours / 24;
        var weeks = days / 7;
        var months = days / 30;
        var years = days / 365;

        if (seconds < 60)
        {
            return "şimdi";
        }

        if (minutes < 60)
        {
            return $"{minutes} dakika önce";
        }

        if (hours < 24)
        {
            return $"{hours} saat önce";
        }

        if (days == 1)
        {
            return "dün";
        }

        if (days < 7)
        {
            return $"{days} gün önce";
        }

        if (weeks < 4)
        {
            return $"{weeks} hafta önce";
        }

        if (months < 12)
        {
            return $"{months} ay önce";
        }

        return $"{years} yıl önce";
    }
}
