using PsikoAl.Data.Entities;

namespace PsikoAl.Services.Tests.Profiles;

// Privacy by default: iletişim bilgisi kullanıcı açıkça açmadıkça paylaşılmaz.
// DB varsayılanı 20261014100100_profile_share_defaults ile aynı yönde.
public sealed class ProfileDefaultsTests
{
    [Fact]
    public void Contact_sharing_is_off_by_default()
    {
        var profile = new Profile { Email = "yeni@example.test" };

        Assert.False(profile.ShareEmail);
        Assert.False(profile.SharePhone);
        Assert.False(profile.ShareLocation);
    }
}
