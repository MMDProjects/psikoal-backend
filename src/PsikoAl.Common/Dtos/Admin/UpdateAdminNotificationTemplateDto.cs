namespace PsikoAl.Common.Dtos.Admin;

public sealed record UpdateAdminNotificationTemplateDto(
    string Title,
    string Body,
    string? HtmlBody,
    bool PushEnabled,
    bool EmailEnabled,
    bool InAppEnabled,
    bool IsActive);
