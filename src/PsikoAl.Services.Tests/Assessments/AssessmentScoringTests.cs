using PsikoAl.Common.Constants;
using PsikoAl.Common.Dtos.Assessment.Create;
using PsikoAl.Common.Exceptions;
using PsikoAl.Data.Entities;

namespace PsikoAl.Services.Tests.Assessments;

// Puan istemcinin yolladığı sayılardan değil, testin gerçek soru ve seçeneklerinden çıkar.
public sealed class AssessmentScoringTests
{
    private const string Options = """[{"id":"a","text":"Hiç","value":0},{"id":"b","text":"Bazen","value":2},{"id":"c","text":"Sık","value":5}]""";

    private static readonly AssessmentQuestion Single1 = NewQuestion("single_choice");
    private static readonly AssessmentQuestion Single2 = NewQuestion("scale");
    private static readonly AssessmentQuestion Multi = NewQuestion(AssessmentScoring.MultipleChoice);

    [Fact]
    public void Sums_option_values_including_every_multiple_choice_selection()
    {
        var score = AssessmentScoring.Score(
            [Single1, Single2, Multi],
            [Answer(Single1, 2), Answer(Single2, 5), Answer(Multi, 2, 5)]);

        Assert.Equal(14, score);
    }

    public static TheoryData<AssessmentAnswerDto[]> InvalidAnswerSets() => new()
    {
        { [Answer(Single1, 2), Answer(Single2, 5)] },
        { [Answer(Single1, 2), Answer(Single2, 5), Answer(Multi, 2), Answer(Multi, 5)] },
        { [Answer(Single1, 2), Answer(Single1, 5), Answer(Multi, 2)] },
        { [new AssessmentAnswerDto("bu-soru-yok", [2]), Answer(Single2, 5), Answer(Multi, 2)] },
        { [new AssessmentAnswerDto(Guid.NewGuid().ToString(), [2]), Answer(Single2, 5), Answer(Multi, 2)] },
        { [Answer(Single1, 99), Answer(Single2, 5), Answer(Multi, 2)] },
        { [Answer(Single1, 2, 5), Answer(Single2, 5), Answer(Multi, 2)] },
        { [Answer(Single1), Answer(Single2, 5), Answer(Multi, 2)] },
        { [Answer(Single1, 2), Answer(Single2, 5), Answer(Multi, 5, 5)] },
    };

    [Theory]
    [MemberData(nameof(InvalidAnswerSets))]
    public void Rejects_answers_that_do_not_fit_the_assessment(AssessmentAnswerDto[] answers)
    {
        var exception = Assert.Throws<DomainException>(() => AssessmentScoring.Score([Single1, Single2, Multi], answers));

        Assert.Equal(ErrorKeys.AssessmentAnswersInvalid, exception.ErrorKey);
        Assert.Equal("answers", exception.Field);
    }

    private static AssessmentQuestion NewQuestion(string type)
        => new() { Id = Guid.NewGuid(), Text = "Soru", Type = type, Options = Options };

    private static AssessmentAnswerDto Answer(AssessmentQuestion question, params int[] values)
        => new(question.Id.ToString(), values);
}
