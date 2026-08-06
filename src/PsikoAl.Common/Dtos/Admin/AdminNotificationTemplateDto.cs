namespace PsikoAl.Common.Dtos.Admin;

public sealed record AdminNotificationTemplateDto(
    Guid Id,
    string Type,
    string Title,
    string Body,
    string? HtmlBody,
    bool PushEnabled,
    bool EmailEnabled,
    bool InAppEnabled,
    bool IsActive);
