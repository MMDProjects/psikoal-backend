namespace PsikoAl.Common.Dtos.Assessment;

public sealed record QuestionDto(Guid Id, string Text, string Type, IReadOnlyList<AnswerOptionDto> Options);
