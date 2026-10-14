using System.Globalization;
using PsikoAl.Common.Constants;

namespace PsikoAl.Common.Presentation;

public static class CreditLabelFormatter
{
    private static readonly CultureInfo TurkishCulture = CultureInfo.GetCultureInfo("tr-TR");

    private static readonly Dictionary<string, string> TypeLabels = new(StringComparer.Ordinal)
    {
        [CreditTransactionTypes.Purchase] = "Kredi yükleme",
        [CreditTransactionTypes.OfferSpend] = "Teklif gönderimi",
        [CreditTransactionTypes.AdminAdjust] = "Yönetici düzeltmesi",
    };

    public static string Balance(int credits) => $"{credits.ToString("N0", TurkishCulture)} kredi";

    public static string Price(decimal amountTry) => $"{amountTry.ToString("N2", TurkishCulture)} ₺";

    public static string UnitPrice(decimal priceTry, int credits)
        => credits <= 0 ? Price(priceTry) : $"{Price(decimal.Round(priceTry / credits, 2))} / kredi";

    public static string TypeLabel(string type) => TypeLabels.GetValueOrDefault(type, type);
}
