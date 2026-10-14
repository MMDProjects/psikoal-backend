using PsikoAl.Common.Constants;
using PsikoAl.Common.Exceptions;
using PsikoAl.Common.Presentation;
using PsikoAl.Data.Entities;
using PsikoAl.Data.Repositories.Abstractions;
using PsikoAl.Services.Abstractions;
using PsikoAl.Services.Mapping;

namespace PsikoAl.Services.Tests.Credits;

public sealed class CreditPresentationTests
{
    [Theory]
    [InlineData(0, "0 kredi")]
    [InlineData(12, "12 kredi")]
    [InlineData(1500, "1.500 kredi")]
    public void Balance_label_is_turkish(int credits, string expected)
        => Assert.Equal(expected, CreditLabelFormatter.Balance(credits));

    [Theory]
    [InlineData("500", "500,00 ₺")]
    [InlineData("1250.5", "1.250,50 ₺")]
    public void Price_label_uses_turkish_number_format(string price, string expected)
        => Assert.Equal(expected, CreditLabelFormatter.Price(decimal.Parse(price, System.Globalization.CultureInfo.InvariantCulture)));

    [Fact]
    public void Unit_price_label_divides_price_by_credits()
        => Assert.Equal("25,00 ₺ / kredi", CreditLabelFormatter.UnitPrice(500m, 20));

    [Theory]
    [InlineData(CreditTransactionTypes.Purchase, "Kredi yükleme")]
    [InlineData(CreditTransactionTypes.OfferSpend, "Teklif gönderimi")]
    [InlineData(CreditTransactionTypes.AdminAdjust, "Yönetici düzeltmesi")]
    public void Every_transaction_type_has_a_turkish_label(string type, string expected)
        => Assert.Equal(expected, CreditLabelFormatter.TypeLabel(type));

    [Fact]
    public void Offer_spend_description_names_the_listing()
    {
        var row = new CreditTransactionReadModel(
            Guid.NewGuid(), -1, 4, CreditTransactionTypes.OfferSpend, Guid.NewGuid(), null, null, "Kaygı için destek", DateTimeOffset.UtcNow);

        Assert.Equal("\"Kaygı için destek\" ilanına teklif gönderildi", CreditMapper.DescriptionFor(row));
    }

    [Fact]
    public void Purchase_description_names_the_package()
    {
        var row = new CreditTransactionReadModel(
            Guid.NewGuid(), 20, 24, CreditTransactionTypes.Purchase, null, null, "Başlangıç", null, DateTimeOffset.UtcNow);

        Assert.Equal("Başlangıç paketi satın alındı", CreditMapper.DescriptionFor(row));
    }

    [Theory]
    [InlineData("0")]
    [InlineData("1")]
    [InlineData("1000")]
    public async Task Offer_cost_setting_accepts_whole_numbers_in_range(string value)
    {
        var (service, setting) = CreateSettingsService();

        await service.UpdateAsync(AdminAuthUserId, SystemSettingKeys.OfferCreditCost, value, CancellationToken.None);

        Assert.Equal(value, setting.Value);
    }

    [Theory]
    [InlineData("-1")]
    [InlineData("1.5")]
    [InlineData("bir")]
    [InlineData("1001")]
    public async Task Offer_cost_setting_rejects_invalid_values(string value)
    {
        var (service, setting) = CreateSettingsService();

        var exception = await Assert.ThrowsAsync<DomainException>(
            () => service.UpdateAsync(AdminAuthUserId, SystemSettingKeys.OfferCreditCost, value, CancellationToken.None));

        Assert.Equal(ErrorKeys.ValidationFailed, exception.ErrorKey);
        Assert.Equal("1", setting.Value);
    }

    private static readonly Guid AdminAuthUserId = Guid.NewGuid();

    private static (SystemSettingsService Service, SystemSetting Setting) CreateSettingsService()
    {
        var setting = new SystemSetting { Key = SystemSettingKeys.OfferCreditCost, Value = "1" };
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var settings = Substitute.For<ISystemSettingRepository>();
        unitOfWork.SystemSettings.Returns(settings);
        unitOfWork.AuditLogs.Returns(Substitute.For<IAuditLogRepository>());
        settings.GetAsync(SystemSettingKeys.OfferCreditCost, Arg.Any<CancellationToken>()).Returns(setting);

        var guard = Substitute.For<IAdminGuard>();
        guard.GetActiveAdminAsync(AdminAuthUserId, Arg.Any<CancellationToken>())
            .Returns(new AdminUser { Id = Guid.NewGuid(), AuthUserId = AdminAuthUserId, DisplayName = "Admin", Role = AdminRoles.SuperAdmin });

        return (new SystemSettingsService(unitOfWork, guard), setting);
    }
}
