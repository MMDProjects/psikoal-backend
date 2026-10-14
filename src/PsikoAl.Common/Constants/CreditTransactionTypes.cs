namespace PsikoAl.Common.Constants;

public static class CreditTransactionTypes
{
    public const string Purchase = "PURCHASE";
    public const string OfferSpend = "OFFER_SPEND";
    public const string AdminAdjust = "ADMIN_ADJUST";

    public static readonly IReadOnlyList<string> All = [Purchase, OfferSpend, AdminAdjust];
}
