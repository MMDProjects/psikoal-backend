using PsikoAl.Common.Presentation;

namespace PsikoAl.Services.Tests.Presentation;

public sealed class EmailMaskerTests
{
    [Theory]
    [InlineData("ayse.yilmaz@gmail.com", "a***@gmail.com")]
    [InlineData("x@psikoal.test", "x***@psikoal.test")]
    [InlineData("gecersiz", "***")]
    [InlineData("@alan.com", "***")]
    [InlineData("", "***")]
    [InlineData(null, "***")]
    public void Masks_the_local_part(string? email, string expected)
        => Assert.Equal(expected, EmailMasker.Mask(email));
}
