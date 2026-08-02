using FluentValidation;
using PsikoAl.Common.Constants;
using PsikoAl.Common.Dtos.Auth.Create;

namespace PsikoAl.Api.Validators;

public sealed class RegisterRequestDtoValidator : AbstractValidator<RegisterRequestDto>
{
    public RegisterRequestDtoValidator()
    {
        RuleFor(request => request.Email).NotEmpty().EmailAddress().WithMessage(ErrorKeys.AuthEmailInvalid);
        RuleFor(request => request.Password).MinimumLength(8).WithMessage(ErrorKeys.AuthWeakPassword);
        RuleFor(request => request.FirstName).NotEmpty().WithMessage(ErrorKeys.ValidationFailed);
        RuleFor(request => request.LastName).NotEmpty().WithMessage(ErrorKeys.ValidationFailed);
        RuleFor(request => request.Role)
            .Must(role => role is ProfileRoles.Expert or ProfileRoles.Client)
            .WithMessage(ErrorKeys.ValidationFailed);
    }
}
