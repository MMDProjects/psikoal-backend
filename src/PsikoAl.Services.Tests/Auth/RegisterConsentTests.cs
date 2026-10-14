using PsikoAl.Common.Dtos.Auth.Create;
using PsikoAl.Services.Options;
using PsikoAl.Services.Tests.Support;

namespace PsikoAl.Services.Tests.Auth;

// KVKK m.6: sağlık verisi açık rızayla işlenir ve rızanın ispatı veri sorumlusundadır.
// Kayıt, rızanın zamanını ve hangi metin sürümüne verildiğini profile yazmalı.
public sealed class RegisterConsentTests
{
    [Fact]
    public async Task Register_records_consent_timestamps_and_configured_version()
    {
        using var database = new TestDatabase();
        var profile = await database.AddProfileAsync();
        var admin = new FakeSupabaseAdminService { UserIdToCreate = profile.Id };
        var service = new AuthService(
            new FakeSupabaseAuthService(profile.Id),
            admin,
            database.UnitOfWork,
            Microsoft.Extensions.Options.Options.Create(new LegalOptions { ConsentVersion = "2026-10-test" }));
        var before = DateTimeOffset.UtcNow;

        await service.RegisterAsync(
            new RegisterRequestDto(profile.Email, "GucluSifre123", "Ayşe", "Yılmaz", "client", true, true),
            CancellationToken.None);

        var stored = Assert.Single(database.Context.Profiles);
        Assert.NotNull(stored.TermsAcceptedAt);
        Assert.NotNull(stored.HealthDataConsentAt);
        Assert.True(stored.TermsAcceptedAt >= before);
        Assert.Equal("2026-10-test", stored.ConsentVersion);
    }

    [Fact]
    public void Consent_version_defaults_to_contract_value()
        => Assert.Equal("2026-10", new LegalOptions().ConsentVersion);
}
