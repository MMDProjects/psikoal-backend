using FluentValidation;
using PsikoAl.Common.Constants;
using PsikoAl.Common.Dtos.Listing.Create;

namespace PsikoAl.Api.Validators;

public sealed class CreateListingDtoValidator : AbstractValidator<CreateListingDto>
{
    private static readonly string[] ValidSessionTypes = ["online", "yüz_yüze", "yüz_yüze_online"];

    public CreateListingDtoValidator()
    {
        RuleFor(request => request.Title).Length(10, 100).WithMessage(ErrorKeys.ValidationFailed);
        RuleFor(request => request.Description).MaximumLength(500).WithMessage(ErrorKeys.ValidationFailed);
        RuleFor(request => request.Specialization).NotEmpty().WithMessage(ErrorKeys.ValidationFailed);
        RuleFor(request => request.BudgetMin).GreaterThan(0).WithMessage(ErrorKeys.ValidationFailed);
        RuleFor(request => request.BudgetMax)
            .GreaterThanOrEqualTo(request => request.BudgetMin)
            .WithMessage(ErrorKeys.ValidationFailed);
        RuleFor(request => request.PreferredSessionType)
            .Must(sessionType => ValidSessionTypes.Contains(sessionType))
            .WithMessage(ErrorKeys.ValidationFailed);
    }
}
