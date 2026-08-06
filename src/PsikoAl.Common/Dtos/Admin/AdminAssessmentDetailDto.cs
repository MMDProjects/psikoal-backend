using PsikoAl.Common.Dtos.Assessment;

namespace PsikoAl.Common.Dtos.Admin;

public sealed record AdminAssessmentDetailDto(
    Guid Id,
    string Title,
    string Description,
    string Category,
    int EstimatedMinutes,
    bool IsActive,
    int SortOrder,
    IReadOnlyList<QuestionDto> Questions,
    IReadOnlyList<AdminScoreRuleDto> ScoreRules);
