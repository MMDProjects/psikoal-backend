using FluentValidation;
using PsikoAl.Common.Constants;
using PsikoAl.Common.Dtos.Expert.Create;
using PsikoAl.Services.Abstractions;

namespace PsikoAl.Api.Validators;

public sealed class CreateExpertProfileDtoValidator : AbstractValidator<CreateExpertProfileDto>
{
    public CreateExpertProfileDtoValidator(ICategoryService categoryService)
    {
        RuleFor(request => request.Title).MinimumLength(2).WithMessage(ErrorKeys.ValidationFailed);
        RuleFor(request => request.Specializations)
            .NotEmpty()
            .WithMessage(ErrorKeys.ValidationFailed)
            // Uzmanlık sözlüğünün tek kaynağı categories tablosu (docs/ADMIN_PANEL_REQUIREMENTS.md §2.6).
            .MustAsync((specializations, cancellationToken) =>
                categoryService.AllActiveNamesExistAsync(specializations, cancellationToken))
            .WithMessage(ErrorKeys.ValidationFailed);
        RuleFor(request => request.ExperienceYears).InclusiveBetween(0, 50).WithMessage(ErrorKeys.ValidationFailed);
        RuleFor(request => request.Bio).Length(50, 1000).WithMessage(ErrorKeys.ValidationFailed);
    }
}
