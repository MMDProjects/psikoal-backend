using FluentValidation;
using PsikoAl.Common.Constants;
using PsikoAl.Common.Dtos.Auth.Create;

namespace PsikoAl.Api.Validators;

public sealed class ChangePasswordRequestDtoValidator : AbstractValidator<ChangePasswordRequestDto>
{
    public ChangePasswordRequestDtoValidator()
    {
        RuleFor(request => request.CurrentPassword).NotEmpty().WithMessage(ErrorKeys.ValidationFailed);
        RuleFor(request => request.NewPassword).MinimumLength(8).WithMessage(ErrorKeys.AuthWeakPassword);
        RuleFor(request => request.ConfirmPassword)
            .Equal(request => request.NewPassword)
            .WithMessage(ErrorKeys.ValidationFailed);
    }
}
