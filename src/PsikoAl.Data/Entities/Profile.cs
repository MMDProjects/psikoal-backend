using PsikoAl.Common.Constants;

namespace PsikoAl.Data.Entities;

public sealed class Profile
{
    public Guid Id { get; set; }

    public required string Email { get; set; }

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string Role { get; set; } = ProfileRoles.Client;

    public bool IsVerified { get; set; }

    public string? AvatarUrl { get; set; }

    public string? Phone { get; set; }

    public string? City { get; set; }

    public bool ShareEmail { get; set; } = true;

    public bool SharePhone { get; set; } = true;

    public bool ShareLocation { get; set; } = true;

    public string Status { get; set; } = ProfileStatuses.Active;

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}
