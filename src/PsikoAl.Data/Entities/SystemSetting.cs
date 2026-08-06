namespace PsikoAl.Data.Entities;

public sealed class SystemSetting
{
    public required string Key { get; set; }

    public required string Value { get; set; }

    public string? Description { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public Guid? UpdatedBy { get; set; }
}
