namespace PsikoAl.Common.Dtos.Notification;

public sealed record NotificationDto(
    Guid Id,
    string Type,
    string Title,
    string Body,
    DateTimeOffset CreatedAt,
    string TimeLabel,
    bool Read);
