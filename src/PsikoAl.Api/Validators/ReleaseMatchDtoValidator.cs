using FluentValidation;
using PsikoAl.Common.Constants;
using PsikoAl.Common.Dtos.Match;

namespace PsikoAl.Api.Validators;

public sealed class ReleaseMatchDtoValidator : AbstractValidator<ReleaseMatchDto>
{
    public ReleaseMatchDtoValidator()
    {
        RuleFor(request => request.MatchId).NotEmpty().WithMessage(ErrorKeys.ValidationFailed);
        RuleFor(request => request.Reason).MaximumLength(500).WithMessage(ErrorKeys.ValidationFailed);
    }
}
