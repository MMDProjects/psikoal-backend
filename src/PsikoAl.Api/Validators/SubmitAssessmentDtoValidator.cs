using FluentValidation;
using PsikoAl.Common.Constants;
using PsikoAl.Common.Dtos.Assessment.Create;

namespace PsikoAl.Api.Validators;

// Cevapların teste uygunluğu (soru kimliği, seçenek değeri) serviste, testin gerçek
// sorularına karşı doğrulanır; burada yalnız biçim ve boyut sınırı var. `email` alanı
// artık saklanmadığı için doğrulanmaz.
public sealed class SubmitAssessmentDtoValidator : AbstractValidator<SubmitAssessmentDto>
{
    private const int MaxAnswers = 200;
    private const int MaxValuesPerAnswer = 20;

    public SubmitAssessmentDtoValidator()
    {
        RuleFor(request => request.AssessmentId).NotEmpty().WithMessage(ErrorKeys.ValidationFailed);
        RuleFor(request => request.Answers)
            .NotEmpty()
            .Must(answers => answers.Count <= MaxAnswers)
            .WithMessage(ErrorKeys.ValidationFailed);
        RuleForEach(request => request.Answers).ChildRules(answer =>
        {
            answer.RuleFor(item => item.QuestionId).NotEmpty().WithMessage(ErrorKeys.ValidationFailed);
            answer.RuleFor(item => item.Values)
                .NotEmpty()
                .Must(values => values.Count <= MaxValuesPerAnswer)
                .WithMessage(ErrorKeys.ValidationFailed);
        });
    }
}
