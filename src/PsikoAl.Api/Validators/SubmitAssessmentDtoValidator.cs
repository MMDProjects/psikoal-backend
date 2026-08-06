using FluentValidation;
using PsikoAl.Common.Constants;
using PsikoAl.Common.Dtos.Assessment.Create;

namespace PsikoAl.Api.Validators;

public sealed class SubmitAssessmentDtoValidator : AbstractValidator<SubmitAssessmentDto>
{
    public SubmitAssessmentDtoValidator()
    {
        RuleFor(request => request.AssessmentId).NotEmpty().WithMessage(ErrorKeys.ValidationFailed);
        RuleFor(request => request.Answers).NotEmpty().WithMessage(ErrorKeys.ValidationFailed);
        RuleFor(request => request.Email).EmailAddress().When(request => request.Email is not null)
            .WithMessage(ErrorKeys.ValidationFailed);
    }
}
