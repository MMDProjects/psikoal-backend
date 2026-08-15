using PsikoAl.Common.Presentation;

namespace PsikoAl.Services.Tests.Presentation;

// Sözleşme kilidi: psikoal-app / mock-db/helpers.js -> formatTRY + budgetLabelOf.
// Kültür tr-TR'ye sabitlenmiştir; CI makinesinin locale'i etiketi değiştirmemeli.
public sealed class BudgetLabelFormatterTests
{
    [Theory]
    [InlineData(1500, 1500, "₺1.500")]
    [InlineData(1000, 2500, "₺1.000 – ₺2.500")]
    [InlineData(0, 0, "₺0")]
    [InlineData(1500.5, 1500.5, "₺1.500,5")]
    public void Format_RendersTheTurkishBudgetLabel(decimal min, decimal max, string expected)
        => Assert.Equal(expected, BudgetLabelFormatter.Format(min, max));
}
