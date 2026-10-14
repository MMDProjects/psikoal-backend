using System.Reflection;
using PsikoAl.Common.Constants;

namespace PsikoAl.Services.Tests.Presentation;

// Sözleşme kilidi: psikoal-app / CLAUDE.md — "ApiError.message: kullanıcıya
// gösterilebilir mesaj (TR)". DomainExceptionMiddleware eskiden bu alana hata
// KODUNU yazıyordu ve mobil onu olduğu gibi ekrana basıyordu: kullanıcı şifre
// ekranında "AUTH_INVALID_CREDENTIALS", ilan ekranında
// "LISTING_MAX_ACTIVE_EXCEEDED" görüyordu. Tek bir ekranın değil, TÜM hata
// yollarının sorunuydu.
public sealed class ErrorMessagesTrTests
{
    /// <summary>Tüm ErrorKeys sabitleri — yansımayla okunur, elle listelenmez.</summary>
    /// <remarks>
    /// Elle yazılmış bir liste, yeni bir ErrorKey eklendiğinde sessizce eksik kalır
    /// ve testin tek işi tam bu kaçağı yakalamaktır. Yansıma, listeyi kaynağın
    /// kendisine bağlar.
    /// </remarks>
    public static TheoryData<string> AllErrorKeys()
    {
        var data = new TheoryData<string>();
        foreach (var field in typeof(ErrorKeys).GetFields(BindingFlags.Public | BindingFlags.Static))
        {
            if (field is { IsLiteral: true, FieldType: { } type } && type == typeof(string))
            {
                data.Add((string)field.GetRawConstantValue()!);
            }
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(AllErrorKeys))]
    public void For_ReturnsATurkishMessageForEveryErrorKey(string errorKey)
    {
        var message = ErrorMessagesTr.For(errorKey);

        Assert.False(string.IsNullOrWhiteSpace(message));

        // Kodun kendisi mesaj OLARAK dönmemeli — kaçağın tam belirtisi buydu.
        Assert.NotEqual(errorKey, message);

        // Genel mesaja düşmüş olmamalı: yeni bir kod eklenip tabloya yazılmazsa
        // For() InternalError mesajına düşer ve kullanıcıya ham kod göstermez,
        // ama eksikliği de gizler. Bu iddia eksikliği geliştirici tarafında görünür
        // kılıyor. InternalError'un kendisi doğal olarak muaf.
        if (errorKey != ErrorKeys.InternalError)
        {
            Assert.NotEqual(ErrorMessagesTr.For(ErrorKeys.InternalError), message);
        }

        // Mesaj makine kodu gibi görünmemeli: ErrorKeys biçimi BÜYÜK_HARF_ALT_ÇİZGİ.
        Assert.DoesNotContain('_', message);
    }

    [Fact]
    public void For_FallsBackInsteadOfLeakingAnUnknownCode()
    {
        var message = ErrorMessagesTr.For("BRAND_NEW_UNMAPPED_KEY");

        Assert.Equal(ErrorMessagesTr.For(ErrorKeys.InternalError), message);
        Assert.DoesNotContain("BRAND_NEW", message);
    }
}
