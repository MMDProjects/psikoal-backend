using FluentValidation;
using PsikoAl.Common.Constants;
using PsikoAl.Common.Dtos.Category.Create;

namespace PsikoAl.Api.Validators;

public sealed class CreateCategoryDtoValidator : AbstractValidator<CreateCategoryDto>
{
    public CreateCategoryDtoValidator()
    {
        RuleFor(request => request.Slug).NotEmpty().Matches("^[a-z0-9-]+$").WithMessage(ErrorKeys.ValidationFailed);
        RuleFor(request => request.Name).NotEmpty().WithMessage(ErrorKeys.ValidationFailed);
        RuleFor(request => request.Icon).NotEmpty().WithMessage(ErrorKeys.ValidationFailed);
        RuleFor(request => request.Summary).NotEmpty().WithMessage(ErrorKeys.ValidationFailed);
        RuleFor(request => request.Description).NotEmpty().WithMessage(ErrorKeys.ValidationFailed);
        RuleFor(request => request.BlogTag).NotEmpty().WithMessage(ErrorKeys.ValidationFailed);
    }
}
