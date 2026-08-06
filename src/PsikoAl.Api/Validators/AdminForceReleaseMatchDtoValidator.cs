using FluentValidation;
using PsikoAl.Common.Constants;
using PsikoAl.Common.Dtos.Admin;

namespace PsikoAl.Api.Validators;

public sealed class AdminForceReleaseMatchDtoValidator : AbstractValidator<AdminForceReleaseMatchDto>
{
    public AdminForceReleaseMatchDtoValidator()
    {
        RuleFor(request => request.TargetStatus)
            .Must(status => status is "RELEASED" or "COMPLETED")
            .WithMessage(ErrorKeys.ValidationFailed);
        RuleFor(request => request.Reason).NotEmpty().MaximumLength(500).WithMessage(ErrorKeys.ValidationFailed);
    }
}
