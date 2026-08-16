using System.Globalization;

namespace PsikoAl.Common.Presentation;

// Davranış referansı: psikoal-app reposu, mock-db/helpers.js -> formatTRY + budgetLabelOf.
// Sözleşme, bu dosyanın unit testlerinde kilitlenir (PsikoAl.Services.Tests/Presentation).
public static class BudgetLabelFormatter
{
    private static readonly CultureInfo TurkishCulture = CultureInfo.GetCultureInfo("tr-TR");

    public static string Format(decimal min, decimal max)
        => min == max ? FormatTry(min) : $"{FormatTry(min)} – {FormatTry(max)}";

    private static string FormatTry(decimal value) => $"₺{value.ToString("#,##0.##", TurkishCulture)}";
}
