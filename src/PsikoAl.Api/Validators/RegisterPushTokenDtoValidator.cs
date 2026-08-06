using FluentValidation;
using PsikoAl.Common.Constants;
using PsikoAl.Common.Dtos.Notification;

namespace PsikoAl.Api.Validators;

public sealed class RegisterPushTokenDtoValidator : AbstractValidator<RegisterPushTokenDto>
{
    public RegisterPushTokenDtoValidator()
    {
        RuleFor(request => request.Token).NotEmpty().WithMessage(ErrorKeys.ValidationFailed);
        RuleFor(request => request.Platform)
            .Must(platform => platform is "ios" or "android")
            .WithMessage(ErrorKeys.ValidationFailed);
    }
}
