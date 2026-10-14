using PsikoAl.Common.Constants;
using PsikoAl.Common.Dtos.Assessment.Create;
using PsikoAl.Common.Exceptions;
using PsikoAl.Data.Entities;
using PsikoAl.Services.Mapping;

namespace PsikoAl.Services;

/// <summary>Gönderilen cevapları testin sorularına göre doğrular ve puanı sunucuda hesaplar.</summary>
/// <remarks>
/// Eskiden QuestionId'ler hiç okunmuyordu ve puan istemcinin yolladığı sayıların
/// toplamıydı: uydurma soru kimlikleri ya da seçenekte olmayan değerlerle istenen
/// seviye (ve uzmanlara gösterilen sonuç) üretilebiliyordu. Artık her soru tam bir kez
/// cevaplanmalı ve her değer o sorunun seçeneklerinden biri olmalı. multiple_choice
/// sorularda seçilen tüm değerler toplanır (mobil useAssessmentEngine değerleri
/// biriktiriyor; önceki "yalnız ilk değer" davranışı ikinci seçimi yutuyordu).
/// </remarks>
public static class AssessmentScoring
{
    public const string MultipleChoice = "multiple_choice";

    public static int Score(IReadOnlyList<AssessmentQuestion> questions, IReadOnlyList<AssessmentAnswerDto> answers)
    {
        if (questions.Count == 0 || answers.Count != questions.Count)
        {
            throw Invalid();
        }

        var answersByQuestion = new Dictionary<Guid, AssessmentAnswerDto>();
        foreach (var answer in answers)
        {
            if (!Guid.TryParse(answer.QuestionId, out var questionId) || !answersByQuestion.TryAdd(questionId, answer))
            {
                throw Invalid();
            }
        }

        var score = 0;
        foreach (var question in questions)
        {
            if (!answersByQuestion.TryGetValue(question.Id, out var answer))
            {
                throw Invalid();
            }

            score += ScoreAnswer(question, answer.Values);
        }

        return score;
    }

    private static int ScoreAnswer(AssessmentQuestion question, IReadOnlyList<int> values)
    {
        var allowsMany = question.Type == MultipleChoice;
        if (values.Count == 0 || (!allowsMany && values.Count != 1) || values.Distinct().Count() != values.Count)
        {
            throw Invalid();
        }

        var optionValues = AssessmentMapper.ToQuestionDto(question).Options.Select(option => option.Value).ToHashSet();
        if (!values.All(optionValues.Contains))
        {
            throw Invalid();
        }

        return values.Sum();
    }

    private static DomainException Invalid() => new(ErrorKeys.AssessmentAnswersInvalid, "answers");
}
