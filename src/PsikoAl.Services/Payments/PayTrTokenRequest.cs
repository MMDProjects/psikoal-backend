namespace PsikoAl.Services.Payments;

public sealed record PayTrTokenRequest(
    string UserIp,
    string MerchantOid,
    string Email,
    int PaymentAmountKurus,
    IReadOnlyList<PayTrBasketItem> Basket,
    string UserName,
    string UserAddress,
    string UserPhone);
