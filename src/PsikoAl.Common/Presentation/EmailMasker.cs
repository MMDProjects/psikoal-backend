namespace PsikoAl.Common.Presentation;

// Loglara tam e-posta adresi yazılmaz (KVKK: log da kişisel veri işleme kaydıdır ve
// log saklama/erişim denetimi veritabanınınkinden gevşektir). Teşhis için ilk harf ve
// alan adı yeterli: "ayse.yilmaz@gmail.com" -> "a***@gmail.com".
public static class EmailMasker
{
    private const string Hidden = "***";

    public static string Mask(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return Hidden;
        }

        var at = email.IndexOf('@', StringComparison.Ordinal);
        return at <= 0
            ? Hidden
            : $"{email[0]}{Hidden}{email[at..]}";
    }
}
