using FluentValidation;
using PsikoAl.Common.Constants;
using PsikoAl.Common.Dtos.Auth.Create;

namespace PsikoAl.Api.Validators;

public sealed class ResetPasswordRequestDtoValidator : AbstractValidator<ResetPasswordRequestDto>
{
    public ResetPasswordRequestDtoValidator()
    {
        RuleFor(request => request.AccessToken).NotEmpty().WithMessage(ErrorKeys.ResetTokenInvalid);
        RuleFor(request => request.NewPassword).MinimumLength(8).WithMessage(ErrorKeys.AuthWeakPassword);
    }
}
