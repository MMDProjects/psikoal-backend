using FluentValidation;
using PsikoAl.Common.Constants;
using PsikoAl.Common.Dtos.Expert.Create;

namespace PsikoAl.Api.Validators;

public sealed class CreateExpertProfileDtoValidator : AbstractValidator<CreateExpertProfileDto>
{
    public CreateExpertProfileDtoValidator()
    {
        RuleFor(request => request.Title).MinimumLength(2).WithMessage(ErrorKeys.ValidationFailed);
        RuleFor(request => request.Specializations).NotEmpty().WithMessage(ErrorKeys.ValidationFailed);
        RuleFor(request => request.ExperienceYears).InclusiveBetween(0, 50).WithMessage(ErrorKeys.ValidationFailed);
        RuleFor(request => request.Bio).Length(50, 1000).WithMessage(ErrorKeys.ValidationFailed);
    }
}
