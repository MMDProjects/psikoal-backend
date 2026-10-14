using System.Security.Cryptography;
using System.Text;

namespace PsikoAl.Services.Payments;

// PayTR iFrame API imzaları:
//   1. adım (get-token):  https://dev.paytr.com/iframe-api/iframe-api-1-adim
//      paytr_token = base64(HMAC-SHA256(merchant_id + user_ip + merchant_oid + email
//                    + payment_amount + user_basket + no_installment + max_installment
//                    + currency + test_mode + merchant_salt, key: merchant_key))
//   2. adım (Bildirim URL): https://dev.paytr.com/iframe-api/iframe-api-2-adim
//      hash = base64(HMAC-SHA256(merchant_oid + merchant_salt + status + total_amount, key: merchant_key))
public static class PayTrHash
{
    public static string IframeToken(PayTrTokenFields fields, string merchantSalt, string merchantKey)
        => Sign(fields.HashInput(merchantSalt), merchantKey);

    public static string CallbackHash(
        string merchantOid,
        string status,
        string totalAmount,
        string merchantSalt,
        string merchantKey)
        => Sign(string.Concat(merchantOid, merchantSalt, status, totalAmount), merchantKey);

    public static bool IsValidCallback(
        string merchantOid,
        string status,
        string totalAmount,
        string receivedHash,
        string merchantSalt,
        string merchantKey)
    {
        var expected = Encoding.UTF8.GetBytes(CallbackHash(merchantOid, status, totalAmount, merchantSalt, merchantKey));
        var received = Encoding.UTF8.GetBytes(receivedHash);
        return CryptographicOperations.FixedTimeEquals(expected, received);
    }

    private static string Sign(string data, string merchantKey)
        => Convert.ToBase64String(
            HMACSHA256.HashData(Encoding.UTF8.GetBytes(merchantKey), Encoding.UTF8.GetBytes(data)));
}
