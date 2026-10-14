using FluentValidation;
using PsikoAl.Common.Constants;

namespace PsikoAl.Api.Validators;

public static class UrlRuleExtensions
{
    // Mobil taraf bu alanlari z.string().url() ile parse ediyor
    // (native-atomic/src/domains/expert/schemas/expert.schema.ts). Goreli bir yol
    // ("avatar.png") ya da semasiz bir alan adi ("psikoal.com") o kurali gecmez:
    // ExpertSchema.parse() firlatir ve uzman detay ekrani bos doner. Deger bu
    // kapida durdurulmazsa hata kullaniciya ancak mobilde, sebebi gorunmeden ulasir.
    public static IRuleBuilderOptions<T, string?> MustBeAbsoluteHttpUrl<T>(
        this IRuleBuilder<T, string?> ruleBuilder)
        => ruleBuilder
            .Must(BeAbsoluteHttpUrl)
            .WithMessage(ErrorKeys.ValidationFailed);

    private static bool BeAbsoluteHttpUrl(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return true;
        }

        return Uri.TryCreate(value, UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }
}
