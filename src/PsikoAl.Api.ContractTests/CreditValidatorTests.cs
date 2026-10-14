using PsikoAl.Api.Validators;
using PsikoAl.Common.Dtos.Admin;

namespace PsikoAl.Api.ContractTests;

public sealed class CreditValidatorTests
{
    private readonly AdminCreditAdjustDtoValidator _adjust = new();
    private readonly CreateAdminCreditPackageDtoValidator _createPackage = new();

    [Theory]
    [InlineData(5, "Hatalı düşüm iadesi")]
    [InlineData(-3, "Mükerrer yükleme düzeltmesi")]
    public void Adjust_with_nonzero_amount_and_reason_is_valid(int amount, string reason)
        => Assert.True(_adjust.Validate(new AdminCreditAdjustDto(amount, reason)).IsValid);

    [Theory]
    [InlineData(5, null)]
    [InlineData(5, "  ")]
    [InlineData(0, "gerekçe")]
    [InlineData(10_001, "gerekçe")]
    public void Adjust_requires_a_reason_and_a_bounded_nonzero_amount(int amount, string? reason)
        => Assert.False(_adjust.Validate(new AdminCreditAdjustDto(amount, reason)).IsValid);

    [Fact]
    public void Package_with_positive_credits_and_price_is_valid()
        => Assert.True(_createPackage.Validate(new CreateAdminCreditPackageDto("Başlangıç", 20, 500m, true, 0)).IsValid);

    [Theory]
    [InlineData("", 20, 500)]
    [InlineData("Başlangıç", 0, 500)]
    [InlineData("Başlangıç", 20, 0)]
    [InlineData("Başlangıç", 20, -1)]
    public void Package_rejects_empty_name_and_non_positive_values(string name, int credits, decimal price)
        => Assert.False(_createPackage.Validate(new CreateAdminCreditPackageDto(name, credits, price, true, 0)).IsValid);

    [Fact]
    public void Package_price_cannot_have_more_than_two_decimals()
        => Assert.False(_createPackage.Validate(new CreateAdminCreditPackageDto("Başlangıç", 20, 500.005m, true, 0)).IsValid);
}
