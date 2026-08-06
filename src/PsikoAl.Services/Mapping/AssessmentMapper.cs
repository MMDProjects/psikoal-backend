using System.Text.Json;
using PsikoAl.Common.Dtos.Assessment;
using PsikoAl.Data.Entities;

namespace PsikoAl.Services.Mapping;

public static class AssessmentMapper
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static AssessmentDto ToAssessmentDto(Assessment assessment, IReadOnlyList<AssessmentQuestion> questions)
        => new(
            assessment.Id,
            assessment.Title,
            assessment.Description,
            [.. questions.Select(ToQuestionDto)],
            assessment.EstimatedMinutes);

    public static QuestionDto ToQuestionDto(AssessmentQuestion question)
        => new(
            question.Id,
            question.Text,
            question.Type,
            JsonSerializer.Deserialize<List<AnswerOptionDto>>(question.Options, JsonOptions) ?? []);

    public static AssessmentResultDto ToResultDto(AssessmentResult result)
        => new(result.Id, result.Score, result.Level, result.Summary, result.Suggestions, result.CreatedAt);

    public static MyAssessmentResultDto ToMyResultDto(AssessmentResult result)
        => new(
            result.Id,
            result.Score,
            result.Level,
            result.Summary,
            result.Suggestions,
            result.CreatedAt,
            result.AssessmentId,
            result.Assessment?.Title ?? string.Empty);
}
