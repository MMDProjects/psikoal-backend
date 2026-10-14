using FluentValidation;
using PsikoAl.Common.Constants;
using PsikoAl.Common.Dtos.Admin;

namespace PsikoAl.Api.Validators;

public sealed class AdminCreditAdjustDtoValidator : AbstractValidator<AdminCreditAdjustDto>
{
    public AdminCreditAdjustDtoValidator()
    {
        RuleFor(request => request.Amount)
            .NotEqual(0)
            .InclusiveBetween(-CreditFieldLimits.AdjustMaxAbsoluteAmount, CreditFieldLimits.AdjustMaxAbsoluteAmount)
            .WithMessage(ErrorKeys.ValidationFailed);
        RuleFor(request => request.Reason)
            .NotEmpty()
            .MaximumLength(CreditFieldLimits.AdjustReasonMaxLength)
            .WithMessage(ErrorKeys.CreditAdjustReasonRequired);
    }
}
