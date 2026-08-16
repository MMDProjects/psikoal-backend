using PsikoAl.Common.Presentation;

namespace PsikoAl.Services.Tests.Presentation;

// Sözleşme kilidi: psikoal-app / mock-db/helpers.js -> maskFullName.
// Eşleşme öncesi uzmana danışanın tam soyadı GİTMEMELİ; bu davranış sessizce değişirse
// KVKK açısından bir sızıntıdır, bu yüzden test seviyesinde sabitlenir.
public sealed class NameMaskerTests
{
    [Theory]
    [InlineData("Zeynep Yılmaz", "Zeynep Y.")]
    [InlineData("Ali Veli Kaya", "Ali V. K.")]
    [InlineData("Zeynep", "Zeynep")]
    [InlineData("  Zeynep   Yılmaz  ", "Zeynep Y.")]
    public void Mask_ShortensEveryNameAfterTheFirst(string input, string expected)
        => Assert.Equal(expected, NameMasker.Mask(input));

    [Fact]
    public void Mask_ReturnsEmpty_ForWhitespaceOnlyInput()
        => Assert.Equal(string.Empty, NameMasker.Mask("   "));
}
