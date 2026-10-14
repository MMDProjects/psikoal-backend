namespace PsikoAl.Data.Entities;

public sealed class CreditPackage
{
    public Guid Id { get; set; }

    public required string Name { get; set; }

    public int Credits { get; set; }

    public decimal PriceTry { get; set; }

    public bool IsActive { get; set; } = true;

    public int SortOrder { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}
