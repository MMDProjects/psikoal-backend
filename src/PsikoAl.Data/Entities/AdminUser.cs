namespace PsikoAl.Data.Entities;

public sealed class AdminUser
{
    public Guid Id { get; set; }

    public Guid AuthUserId { get; set; }

    public required string DisplayName { get; set; }

    public required string Role { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTimeOffset CreatedAt { get; set; }
}
