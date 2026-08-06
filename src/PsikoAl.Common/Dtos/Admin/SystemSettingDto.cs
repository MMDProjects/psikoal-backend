namespace PsikoAl.Common.Dtos.Admin;

public sealed record SystemSettingDto(string Key, string Value, string? Description, DateTimeOffset UpdatedAt);
