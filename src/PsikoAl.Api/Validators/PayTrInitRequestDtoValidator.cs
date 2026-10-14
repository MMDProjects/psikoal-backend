using FluentValidation;
using PsikoAl.Common.Constants;
using PsikoAl.Common.Dtos.Payment.Create;

namespace PsikoAl.Api.Validators;

public sealed class PayTrInitRequestDtoValidator : AbstractValidator<PayTrInitRequestDto>
{
    public PayTrInitRequestDtoValidator()
    {
        RuleFor(request => request.PackageId).NotEmpty().WithMessage(ErrorKeys.ValidationFailed);
    }
}
