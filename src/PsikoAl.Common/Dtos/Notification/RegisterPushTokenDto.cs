namespace PsikoAl.Common.Dtos.Notification;

public sealed record RegisterPushTokenDto(string Token, string Platform, string? DeviceId);
