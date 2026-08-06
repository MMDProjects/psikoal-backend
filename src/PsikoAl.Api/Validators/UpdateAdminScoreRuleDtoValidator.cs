using FluentValidation;
using PsikoAl.Common.Constants;
using PsikoAl.Common.Dtos.Admin;

namespace PsikoAl.Api.Validators;

public sealed class UpdateAdminScoreRuleDtoValidator : AbstractValidator<UpdateAdminScoreRuleDto>
{
    public UpdateAdminScoreRuleDtoValidator()
    {
        RuleFor(request => request.Summary).NotEmpty().MaximumLength(1000).WithMessage(ErrorKeys.ValidationFailed);
    }
}
