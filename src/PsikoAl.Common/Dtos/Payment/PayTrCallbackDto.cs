namespace PsikoAl.Common.Dtos.Payment;

public sealed record PayTrCallbackDto(
    string MerchantOid,
    string Status,
    string TotalAmount,
    string Hash,
    string? FailedReasonCode,
    string? FailedReasonMessage,
    string? TestMode,
    string? PaymentType,
    string? Currency,
    string? PaymentAmount);
