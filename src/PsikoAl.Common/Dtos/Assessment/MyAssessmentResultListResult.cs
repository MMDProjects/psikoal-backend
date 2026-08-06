namespace PsikoAl.Common.Dtos.Assessment;

public sealed record MyAssessmentResultListResult(IReadOnlyList<MyAssessmentResultDto> Data, int Total);
