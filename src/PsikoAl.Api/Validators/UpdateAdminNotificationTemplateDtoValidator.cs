using FluentValidation;
using PsikoAl.Common.Constants;
using PsikoAl.Common.Dtos.Admin;

namespace PsikoAl.Api.Validators;

public sealed class UpdateAdminNotificationTemplateDtoValidator : AbstractValidator<UpdateAdminNotificationTemplateDto>
{
    public UpdateAdminNotificationTemplateDtoValidator()
    {
        RuleFor(request => request.Title).NotEmpty().MaximumLength(150).WithMessage(ErrorKeys.ValidationFailed);
        RuleFor(request => request.Body).NotEmpty().MaximumLength(1000).WithMessage(ErrorKeys.ValidationFailed);
    }
}
