namespace PsikoAl.Services.Payments;

// paytr_token hash'ine giren alanlar, PayTR'nin istediği SIRAYLA. Değerler forma
// birebir aynı metinle yazılır; hash ile form arasında tek karakter fark token'ı geçersiz kılar.
public sealed record PayTrTokenFields(
    string MerchantId,
    string UserIp,
    string MerchantOid,
    string Email,
    string PaymentAmount,
    string UserBasket,
    string NoInstallment,
    string MaxInstallment,
    string Currency,
    string TestMode)
{
    public string HashInput(string merchantSalt)
        => string.Concat(
            MerchantId,
            UserIp,
            MerchantOid,
            Email,
            PaymentAmount,
            UserBasket,
            NoInstallment,
            MaxInstallment,
            Currency,
            TestMode,
            merchantSalt);
}
