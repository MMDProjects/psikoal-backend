using PsikoAl.Api.Validators;
using PsikoAl.Common.Dtos.Auth.Update;

namespace PsikoAl.Api.ContractTests;

public sealed class UpdateProfileDtoValidatorTests
{
    private readonly UpdateProfileDtoValidator _validator = new();

    [Fact]
    public void Empty_patch_is_valid()
        => Assert.True(_validator.Validate(Patch()).IsValid);

    [Theory]
    [InlineData("+905551112233")]
    [InlineData("0555 111 22 33")]
    [InlineData("(0312) 555-12-34")]
    [InlineData("")]
    public void Loose_phone_formats_are_accepted(string phone)
        => Assert.True(_validator.Validate(Patch(phone: phone)).IsValid);

    [Theory]
    [InlineData("telefonum yok")]
    [InlineData("123")]
    [InlineData("+90555111223344556677889")]
    public void Non_phone_content_is_rejected(string phone)
        => Assert.False(_validator.Validate(Patch(phone: phone)).IsValid);

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Name_cannot_be_blanked(string firstName)
        => Assert.False(_validator.Validate(Patch(firstName: firstName)).IsValid);

    [Fact]
    public void Overlong_fields_are_rejected()
    {
        Assert.False(_validator.Validate(Patch(firstName: new string('a', 51))).IsValid);
        Assert.False(_validator.Validate(Patch(city: new string('a', 81))).IsValid);
    }

    private static UpdateProfileDto Patch(string? firstName = null, string? phone = null, string? city = null)
        => new(firstName, LastName: null, phone, city, ShareEmail: null, SharePhone: null, ShareLocation: null);
}
