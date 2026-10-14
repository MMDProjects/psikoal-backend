namespace PsikoAl.Common.Dtos.Payment;

public sealed record PayTrInitResultDto(string IframeToken, string MerchantOid, string IframeUrl);
