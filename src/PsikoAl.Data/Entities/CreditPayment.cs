using PsikoAl.Common.Constants;

namespace PsikoAl.Data.Entities;

public sealed class CreditPayment
{
    public Guid Id { get; set; }

    public Guid ExpertId { get; set; }

    public Guid PackageId { get; set; }

    public required string MerchantOid { get; set; }

    public decimal AmountTry { get; set; }

    public int Credits { get; set; }

    public string Status { get; set; } = CreditPaymentStatuses.Pending;

    public string? ProviderPayload { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? CompletedAt { get; set; }

    public Expert? Expert { get; set; }

    public CreditPackage? Package { get; set; }
}
