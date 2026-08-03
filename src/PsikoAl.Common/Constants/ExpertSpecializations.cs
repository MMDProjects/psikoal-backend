namespace PsikoAl.Common.Constants;

// Geçici tek kaynak: Dilim 3'te categories tablosuna taşınacak (admin CRUD ile).
// Liste native-atomic ExpertSpecializations sabitiyle birebir aynıdır.
public static class ExpertSpecializations
{
    public static readonly IReadOnlyList<string> All =
    [
        "Anksiyete ve Kaygı",
        "Depresyon",
        "İlişki Problemleri",
        "Travma ve TSSB",
        "Bağımlılık",
        "Aile Terapisi",
        "Çift Terapisi",
        "Çocuk ve Ergen",
        "Kişilik Bozuklukları",
        "Yeme Bozuklukları",
        "Uyku Sorunları",
        "Öfke Yönetimi",
        "Özgüven ve Benlik",
        "Kayıp ve Yas",
        "Kariyer ve İş Stresi",
    ];

    public static bool IsValid(string value) => All.Contains(value);
}
