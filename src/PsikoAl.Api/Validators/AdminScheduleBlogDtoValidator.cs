using FluentValidation;
using PsikoAl.Common.Constants;
using PsikoAl.Common.Dtos.Admin;

namespace PsikoAl.Api.Validators;

public sealed class AdminScheduleBlogDtoValidator : AbstractValidator<AdminScheduleBlogDto>
{
    public AdminScheduleBlogDtoValidator()
    {
        RuleFor(request => request.PublishAt).NotEqual(default(DateTimeOffset)).WithMessage(ErrorKeys.ValidationFailed);
    }
}
