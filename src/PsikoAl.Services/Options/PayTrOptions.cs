namespace PsikoAl.Services.Options;

// Anahtarlar YALNIZ ortam değişkeninden gelir (PayTR__MerchantId, PayTR__MerchantKey,
// PayTR__MerchantSalt). appsettings dosyalarına yazılmaz. Üçü de boşsa ödeme uçları
// PAYMENT_PROVIDER_UNAVAILABLE döner ve callback hiçbir bildirimi kabul etmez.
public sealed class PayTrOptions
{
    public const string SectionName = "PayTR";

    public string? MerchantId { get; set; }

    public string? MerchantKey { get; set; }

    public string? MerchantSalt { get; set; }

    public bool TestMode { get; set; } = true;

    public string? OkUrl { get; set; }

    public string? FailUrl { get; set; }

    public int TimeoutLimitMinutes { get; set; } = 30;

    public bool IsConfigured
        => !string.IsNullOrWhiteSpace(MerchantId)
            && !string.IsNullOrWhiteSpace(MerchantKey)
            && !string.IsNullOrWhiteSpace(MerchantSalt);
}
