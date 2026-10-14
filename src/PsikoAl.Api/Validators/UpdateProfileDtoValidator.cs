using FluentValidation;
using PsikoAl.Common.Constants;
using PsikoAl.Common.Dtos.Auth.Update;

namespace PsikoAl.Api.Validators;

// PATCH /auth/me hiçbir doğrulamadan geçmiyordu: ad boşaltılabiliyor, sınırsız uzunlukta
// metin ya da telefon alanına rastgele içerik yazılabiliyordu. Alanlar nullable —
// null "değiştirme" demek, kurallar yalnız değer gönderildiğinde koşar. Telefon ve
// şehirde boş metin bilinçli olarak geçerli: ProfileService onu "temizle" sayıyor.
public sealed partial class UpdateProfileDtoValidator : AbstractValidator<UpdateProfileDto>
{
    private const int MaxNameLength = 50;
    private const int MaxCityLength = 80;

    public UpdateProfileDtoValidator()
    {
        RuleFor(request => request.FirstName)
            .Must(name => !string.IsNullOrWhiteSpace(name))
            .MaximumLength(MaxNameLength)
            .WithMessage(ErrorKeys.ValidationFailed)
            .When(request => request.FirstName is not null);

        RuleFor(request => request.LastName)
            .Must(name => !string.IsNullOrWhiteSpace(name))
            .MaximumLength(MaxNameLength)
            .WithMessage(ErrorKeys.ValidationFailed)
            .When(request => request.LastName is not null);

        RuleFor(request => request.Phone)
            .Matches(PhonePattern())
            .WithMessage(ErrorKeys.ValidationFailed)
            .When(request => !string.IsNullOrEmpty(request.Phone));

        RuleFor(request => request.City)
            .MaximumLength(MaxCityLength)
            .WithMessage(ErrorKeys.ValidationFailed)
            .When(request => request.City is not null);
    }

    // Gevşek: +90, boşluk, tire ve parantez serbest; 7-20 karakter. Biçimi uygulama
    // gösterir, burada yalnız telefon olmayan içerik reddedilir.
    [System.Text.RegularExpressions.GeneratedRegex(@"^\+?[0-9(][0-9 ()\-]{6,19}$")]
    private static partial System.Text.RegularExpressions.Regex PhonePattern();
}
