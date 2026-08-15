namespace PsikoAl.Common.Presentation;

// Davranış referansı: psikoal-app reposu, mock-db/helpers.js -> maskFullName ("Zeynep Yılmaz" → "Zeynep Y.").
// Sözleşme, bu dosyanın unit testlerinde kilitlenir (PsikoAl.Services.Tests/Presentation).
// Eşleşme öncesi uzmana danışanın tam adı hiç gönderilmemeli; bu yalnızca sunum katmanında
// kullanılır, ham fullName API yanıtına asla eklenmez.
public static class NameMasker
{
    public static string Mask(string fullName)
    {
        var parts = fullName.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return string.Join(' ', parts.Select((word, index) => index == 0 ? word : $"{word[0]}."));
    }
}
