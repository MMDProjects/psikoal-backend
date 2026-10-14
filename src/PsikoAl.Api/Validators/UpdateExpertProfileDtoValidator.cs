using FluentValidation;
using PsikoAl.Common.Constants;
using PsikoAl.Common.Dtos.Expert.Update;

namespace PsikoAl.Api.Validators;

// Bu validator YOKTU: PATCH /experts/profile hicbir dogrulamadan gecmiyordu, yani
// POST'ta reddedilen bir deger PATCH ile serbestce yazilabiliyordu. Kurallar
// Create ile ayni; Update'te alanlar nullable oldugu icin her kural yalniz deger
// gonderildiginde kosar (null = "bu alani degistirme").
public sealed class UpdateExpertProfileDtoValidator : AbstractValidator<UpdateExpertProfileDto>
{
    public UpdateExpertProfileDtoValidator()
    {
        RuleFor(request => request.Title)
            .MinimumLength(2)
            .WithMessage(ErrorKeys.ValidationFailed)
            .When(request => request.Title is not null);

        RuleFor(request => request.Specializations)
            .NotEmpty()
            .WithMessage(ErrorKeys.ValidationFailed)
            .When(request => request.Specializations is not null);

        RuleFor(request => request.ExperienceYears!.Value)
            .InclusiveBetween(0, 50)
            .WithMessage(ErrorKeys.ValidationFailed)
            .When(request => request.ExperienceYears.HasValue);

        RuleFor(request => request.Bio)
            .Length(50, 1000)
            .WithMessage(ErrorKeys.ValidationFailed)
            .When(request => request.Bio is not null);

        RuleFor(request => request.PersonalWebsite).MustBeAbsoluteHttpUrl();
        RuleFor(request => request.CvUrl).MustBeAbsoluteHttpUrl();
    }
}
