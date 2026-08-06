using FluentValidation;
using PsikoAl.Common.Constants;
using PsikoAl.Common.Dtos.Admin;

namespace PsikoAl.Api.Validators;

public sealed class UpdateAdminAssessmentDtoValidator : AbstractValidator<UpdateAdminAssessmentDto>
{
    public UpdateAdminAssessmentDtoValidator()
    {
        RuleFor(request => request.Title).NotEmpty().MaximumLength(150).WithMessage(ErrorKeys.ValidationFailed);
        RuleFor(request => request.Category).NotEmpty().MaximumLength(50).WithMessage(ErrorKeys.ValidationFailed);
        RuleFor(request => request.EstimatedMinutes).GreaterThan(0).WithMessage(ErrorKeys.ValidationFailed);
    }
}
