using FluentValidation;
using PsikoAl.Common.Constants;
using PsikoAl.Common.Dtos.Review.Create;

namespace PsikoAl.Api.Validators;

public sealed class CreateReviewDtoValidator : AbstractValidator<CreateReviewDto>
{
    public CreateReviewDtoValidator()
    {
        RuleFor(request => request.MatchId).NotEmpty().WithMessage(ErrorKeys.ValidationFailed);
        RuleFor(request => request.Rating).InclusiveBetween(1, 5).WithMessage(ErrorKeys.ValidationFailed);
        RuleFor(request => request.Comment).NotEmpty().MaximumLength(1000).WithMessage(ErrorKeys.ValidationFailed);
        RuleFor(request => request.SessionType)
            .Must(sessionType => sessionType is null or "online" or "yüz_yüze" or "yüz_yüze_online")
            .WithMessage(ErrorKeys.ValidationFailed);
    }
}
