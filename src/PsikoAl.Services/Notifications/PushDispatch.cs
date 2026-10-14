namespace PsikoAl.Services.Notifications;

public sealed record PushDispatch(Guid UserId, string Title, string Body, string DataJson);
