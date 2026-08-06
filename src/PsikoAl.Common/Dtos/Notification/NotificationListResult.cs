namespace PsikoAl.Common.Dtos.Notification;

public sealed record NotificationListResult(IReadOnlyList<NotificationDto> Data, int Total, int UnreadCount);
