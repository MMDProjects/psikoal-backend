using System.Globalization;
using PsikoAl.Common.Constants;
using PsikoAl.Data.Repositories.Abstractions;

namespace PsikoAl.Services;

public static class OfferCreditCost
{
    public const int Default = 1;

    // Ayar eksik ya da bozuksa varsayılan maliyet uygulanır: yanlış bir panel girdisi
    // teklifleri sessizce ÜCRETSİZ yapmamalı. Geçerli değerler SystemSettingsService'te
    // kaydedilirken doğrulanır.
    public static async Task<int> ReadAsync(ISystemSettingRepository settings, CancellationToken cancellationToken)
    {
        var setting = await settings.GetAsync(SystemSettingKeys.OfferCreditCost, cancellationToken);
        return TryParse(setting?.Value, out var cost) ? cost : Default;
    }

    public static bool TryParse(string? value, out int cost)
        => int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out cost)
            && cost <= CreditFieldLimits.OfferCostMax;
}
