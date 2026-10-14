namespace PsikoAl.Data.Entities;

public sealed class CreditTransaction
{
    public Guid Id { get; set; }

    public Guid ExpertId { get; set; }

    public int Amount { get; set; }

    public int BalanceAfter { get; set; }

    public required string Type { get; set; }

    public Guid? OfferId { get; set; }

    public Guid? PaymentId { get; set; }

    public Guid? AdminUserId { get; set; }

    public string? Reason { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public Offer? Offer { get; set; }

    public CreditPayment? Payment { get; set; }

    public AdminUser? AdminUser { get; set; }
}
