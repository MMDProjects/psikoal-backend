namespace PsikoAl.Common.Dtos.Admin;

// targetType: "user" (userId zorunlu) veya "segment" (role zorunlu, city/specialization opsiyonel filtre).
public sealed record AdminSendNotificationDto(
    string TargetType,
    Guid? UserId,
    string? Role,
    string? City,
    string? Specialization,
    string Title,
    string Body);
