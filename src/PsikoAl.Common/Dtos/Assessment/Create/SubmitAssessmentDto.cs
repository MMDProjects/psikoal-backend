namespace PsikoAl.Common.Dtos.Assessment.Create;

public sealed record SubmitAssessmentDto(Guid AssessmentId, IReadOnlyList<AssessmentAnswerDto> Answers, string? Email);
