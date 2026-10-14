using FluentValidation;
using PsikoAl.Common.Constants;
using PsikoAl.Common.Dtos.Admin;

namespace PsikoAl.Api.Validators;

public sealed class CreateAdminCreditPackageDtoValidator : AbstractValidator<CreateAdminCreditPackageDto>
{
    public CreateAdminCreditPackageDtoValidator()
    {
        RuleFor(request => request.Name)
            .NotEmpty()
            .MaximumLength(CreditFieldLimits.PackageNameMaxLength)
            .WithMessage(ErrorKeys.ValidationFailed);
        RuleFor(request => request.Credits)
            .InclusiveBetween(1, CreditFieldLimits.PackageMaxCredits)
            .WithMessage(ErrorKeys.ValidationFailed);
        RuleFor(request => request.PriceTry)
            .GreaterThan(0m)
            .LessThanOrEqualTo(CreditFieldLimits.PackageMaxPriceTry)
            .PrecisionScale(10, 2, ignoreTrailingZeros: true)
            .WithMessage(ErrorKeys.ValidationFailed);
        RuleFor(request => request.SortOrder).GreaterThanOrEqualTo(0).WithMessage(ErrorKeys.ValidationFailed);
    }
}
