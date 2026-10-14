using PsikoAl.Common.Constants;
using PsikoAl.Common.Dtos.Offer.Create;
using PsikoAl.Common.Exceptions;
using PsikoAl.Data.Entities;
using PsikoAl.Data.Repositories.Abstractions;
using PsikoAl.Services.Abstractions;

namespace PsikoAl.Services.Tests.Credits;

// Teklif gönderimi kredi harcar (Armut modeli). Atomikliğin VERİTABANI yarısı (koşullu
// UPDATE + satır kilidi + transaction geri alma) E2E katmanında ölçülür; burada servis
// sözleşmesi ölçülür: kurallar ÖNCE, düşüm transaction İÇİNDE, yetmezse hiçbir şey yazılmaz.
public sealed class OfferCreditChargeTests
{
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IExpertRepository _experts = Substitute.For<IExpertRepository>();
    private readonly IListingRepository _listings = Substitute.For<IListingRepository>();
    private readonly IOfferRepository _offers = Substitute.For<IOfferRepository>();
    private readonly IReviewRepository _reviews = Substitute.For<IReviewRepository>();
    private readonly ISystemSettingRepository _settings = Substitute.For<ISystemSettingRepository>();
    private readonly IExpertCreditRepository _credits = Substitute.For<IExpertCreditRepository>();
    private readonly ICreditTransactionRepository _ledger = Substitute.For<ICreditTransactionRepository>();
    private readonly IUnitOfWorkTransaction _transaction = Substitute.For<IUnitOfWorkTransaction>();
    private readonly INotificationService _notifications = Substitute.For<INotificationService>();
    private readonly List<CreditTransaction> _ledgerRows = [];
    private readonly OfferService _service;

    private readonly Guid _expertId = Guid.NewGuid();
    private readonly Guid _offerId = Guid.NewGuid();
    private readonly Listing _listing = new()
    {
        Id = Guid.NewGuid(),
        ClientId = Guid.NewGuid(),
        Title = "Kaygı için destek",
        Description = "d",
        PreferredSessionType = "online",
        Status = ListingStatuses.Open,
    };

    public OfferCreditChargeTests()
    {
        _unitOfWork.Experts.Returns(_experts);
        _unitOfWork.Listings.Returns(_listings);
        _unitOfWork.Offers.Returns(_offers);
        _unitOfWork.Reviews.Returns(_reviews);
        _unitOfWork.SystemSettings.Returns(_settings);
        _unitOfWork.ExpertCredits.Returns(_credits);
        _unitOfWork.CreditTransactions.Returns(_ledger);
        _unitOfWork.BeginTransactionAsync(Arg.Any<CancellationToken>()).Returns(_transaction);

        _experts.GetWithProfileAsync(_expertId, Arg.Any<CancellationToken>()).Returns(new Expert
        {
            Id = _expertId,
            Title = "Klinik Psikolog",
            Status = ExpertStatuses.Approved,
            Profile = new Profile { Id = _expertId, Email = "uzman@example.test", FirstName = "Elif", LastName = "Kaya" },
        });
        _listings.GetByIdAsync(_listing.Id, Arg.Any<CancellationToken>()).Returns(_listing);
        _offers.ExistsForListingAndExpertAsync(_listing.Id, _expertId, Arg.Any<CancellationToken>()).Returns(false);
        _offers.AddAsync(Arg.Do<Offer>(offer => offer.Id = _offerId), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        _offers.GetWithListingAndExpertAsync(_offerId, Arg.Any<CancellationToken>()).Returns(call => new Offer
        {
            Id = _offerId,
            ListingId = _listing.Id,
            ExpertId = _expertId,
            Price = 1000m,
            SessionType = "online",
            Listing = _listing,
        });
        _ledger.AddAsync(Arg.Do<CreditTransaction>(_ledgerRows.Add), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        SetCost("1");

        _service = new OfferService(_unitOfWork, _notifications);
    }

    [Fact]
    public async Task Offer_debits_the_cost_and_writes_offer_and_ledger_in_one_committed_transaction()
    {
        _credits.TryApplyDeltaAsync(_expertId, -1, Arg.Any<CancellationToken>()).Returns(11);

        var offer = await _service.CreateAsync(_expertId, Request(), CancellationToken.None);

        Assert.Equal(_offerId, offer.Id);
        var row = Assert.Single(_ledgerRows);
        Assert.Equal(CreditTransactionTypes.OfferSpend, row.Type);
        Assert.Equal(-1, row.Amount);
        Assert.Equal(11, row.BalanceAfter);
        Assert.Equal(_offerId, row.OfferId);
        Assert.Equal(_expertId, row.ExpertId);

        Received.InOrder(() =>
        {
            _unitOfWork.BeginTransactionAsync(Arg.Any<CancellationToken>());
            _credits.TryApplyDeltaAsync(_expertId, -1, Arg.Any<CancellationToken>());
            _offers.AddAsync(Arg.Any<Offer>(), Arg.Any<CancellationToken>());
            _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>());
            _transaction.CommitAsync(Arg.Any<CancellationToken>());
        });
        await _transaction.Received(1).DisposeAsync();
    }

    [Fact]
    public async Task Insufficient_credits_rejects_without_writing_anything()
    {
        _credits.TryApplyDeltaAsync(_expertId, -1, Arg.Any<CancellationToken>()).Returns((int?)null);

        var exception = await Assert.ThrowsAsync<DomainException>(
            () => _service.CreateAsync(_expertId, Request(), CancellationToken.None));

        Assert.Equal(ErrorKeys.InsufficientCredits, exception.ErrorKey);
        await _offers.DidNotReceive().AddAsync(Arg.Any<Offer>(), Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        await _transaction.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
        await _transaction.Received(1).DisposeAsync();
        Assert.Empty(_ledgerRows);
        await _notifications.DidNotReceiveWithAnyArgs().NotifyAsync(default, default!, default!, default, default);
    }

    [Fact]
    public async Task Duplicate_offer_rule_runs_before_any_charge()
    {
        _offers.ExistsForListingAndExpertAsync(_listing.Id, _expertId, Arg.Any<CancellationToken>()).Returns(true);

        var exception = await Assert.ThrowsAsync<DomainException>(
            () => _service.CreateAsync(_expertId, Request(), CancellationToken.None));

        Assert.Equal(ErrorKeys.OfferAlreadyExists, exception.ErrorKey);
        await AssertNoChargeAttemptedAsync();
    }

    [Fact]
    public async Task Closed_listing_rule_runs_before_any_charge()
    {
        _listing.Status = ListingStatuses.Matched;

        var exception = await Assert.ThrowsAsync<DomainException>(
            () => _service.CreateAsync(_expertId, Request(), CancellationToken.None));

        Assert.Equal(ErrorKeys.OfferListingNotOpen, exception.ErrorKey);
        await AssertNoChargeAttemptedAsync();
    }

    [Fact]
    public async Task Unapproved_expert_rule_runs_before_any_charge()
    {
        _experts.GetWithProfileAsync(_expertId, Arg.Any<CancellationToken>()).Returns(new Expert
        {
            Id = _expertId,
            Title = "Klinik Psikolog",
            Status = ExpertStatuses.Pending,
            Profile = new Profile { Id = _expertId, Email = "uzman@example.test" },
        });

        var exception = await Assert.ThrowsAsync<DomainException>(
            () => _service.CreateAsync(_expertId, Request(), CancellationToken.None));

        Assert.Equal(ErrorKeys.OfferExpertNotApproved, exception.ErrorKey);
        await AssertNoChargeAttemptedAsync();
    }

    [Fact]
    public async Task Zero_cost_sends_the_offer_without_touching_the_balance()
    {
        SetCost("0");

        await _service.CreateAsync(_expertId, Request(), CancellationToken.None);

        await _credits.DidNotReceiveWithAnyArgs().TryApplyDeltaAsync(default, default, default);
        Assert.Empty(_ledgerRows);
        await _offers.Received(1).AddAsync(Arg.Any<Offer>(), Arg.Any<CancellationToken>());
        await _transaction.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("3", 3)]
    [InlineData("bozuk", OfferCreditCost.Default)]
    [InlineData("-2", OfferCreditCost.Default)]
    [InlineData(null, OfferCreditCost.Default)]
    public async Task Cost_comes_from_OFFER_CREDIT_COST_and_falls_back_to_the_default(string? value, int expectedCost)
    {
        SetCost(value);
        _credits.TryApplyDeltaAsync(_expertId, -expectedCost, Arg.Any<CancellationToken>()).Returns(5);

        await _service.CreateAsync(_expertId, Request(), CancellationToken.None);

        Assert.Equal(-expectedCost, Assert.Single(_ledgerRows).Amount);
    }

    private void SetCost(string? value)
        => _settings.GetAsync(SystemSettingKeys.OfferCreditCost, Arg.Any<CancellationToken>())
            .Returns(value is null ? null : new SystemSetting { Key = SystemSettingKeys.OfferCreditCost, Value = value });

    private CreateOfferDto Request() => new(_listing.Id, "Teklif", 1000m, "online", null);

    private async Task AssertNoChargeAttemptedAsync()
    {
        await _unitOfWork.DidNotReceive().BeginTransactionAsync(Arg.Any<CancellationToken>());
        await _credits.DidNotReceiveWithAnyArgs().TryApplyDeltaAsync(default, default, default);
        await _offers.DidNotReceive().AddAsync(Arg.Any<Offer>(), Arg.Any<CancellationToken>());
        Assert.Empty(_ledgerRows);
    }
}
