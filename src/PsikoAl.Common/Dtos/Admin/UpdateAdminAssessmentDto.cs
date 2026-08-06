namespace PsikoAl.Common.Dtos.Admin;

public sealed record UpdateAdminAssessmentDto(
    string Title,
    string Description,
    string Category,
    int EstimatedMinutes,
    bool IsActive,
    int SortOrder);
