namespace PsikoAl.Common.Dtos.Assessment.Create;

public sealed record AssessmentAnswerDto(string QuestionId, IReadOnlyList<int> Values);
