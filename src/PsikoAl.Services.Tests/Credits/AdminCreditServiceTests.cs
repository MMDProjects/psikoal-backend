using PsikoAl.Common.Constants;
using PsikoAl.Common.Dtos.Admin;
using PsikoAl.Common.Exceptions;
using PsikoAl.Data.Entities;
using PsikoAl.Data.Repositories.Abstractions;
using PsikoAl.Services.Tests.Admin;
using PsikoAl.Services.Tests.Support;

namespace PsikoAl.Services.Tests.Credits;

public sealed class AdminCreditServiceTests : IDisposable
{
    private readonly AdminTestDoubles _doubles = new(AdminRoles.Finance);
    private readonly TestDatabase _database = new();
    private readonly IExpertRepository _experts = Substitute.For<IExpertRepository>();
    private readonly IExpertCreditRepository _credits = Substitute.For<IExpertCreditRepository>();
    private readonly ICreditTransactionRepository _ledger = Substitute.For<ICreditTransactionRepository>();
    private readonly ICreditPackageRepository _packages = Substitute.For<ICreditPackageRepository>();
    private readonly ICreditPaymentRepository _payments = Substitute.For<ICreditPaymentRepository>();
    private readonly IUnitOfWorkTransaction _transaction = Substitute.For<IUnitOfWorkTransaction>();
    private readonly List<CreditTransaction> _ledgerRows = [];
    private readonly AdminCreditService _service;

    private readonly Guid _expertId = Guid.NewGuid();

    public AdminCreditServiceTests()
    {
        var unitOfWork = _doubles.UnitOfWork;
        unitOfWork.Experts.Returns(_experts);
        unitOfWork.ExpertCredits.Returns(_credits);
        unitOfWork.CreditTransactions.Returns(_ledger);
        unitOfWork.CreditPackages.Returns(_packages);
        unitOfWork.CreditPayments.Returns(_payments);
        unitOfWork.BeginTransactionAsync(Arg.Any<CancellationToken>()).Returns(_transaction);

        _experts.ExistsAsync(_expertId, Arg.Any<CancellationToken>()).Returns(true);
        _experts.GetWithProfileAsync(_expertId, Arg.Any<CancellationToken>()).Returns(new Expert
        {
            Id = _expertId,
            Title = "Klinik Psikolog",
            Profile = new Profile { Id = _expertId, Email = "uzman@example.test", FirstName = "Elif", LastName = "Kaya" },
        });
        _ledger.AddAsync(Arg.Do<CreditTransaction>(_ledgerRows.Add), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        _ledger.QueryForExpert(_expertId).Returns(_ => _database.Context.CreditTransactions.Where(row => row.ExpertId == _expertId));

        _service = new AdminCreditService(unitOfWork, _doubles.AdminGuard);
    }

    [Fact]
    public async Task Adjust_that_would_go_negative_is_rejected_and_writes_nothing()
    {
        _credits.TryApplyDeltaAsync(_expertId, -5, Arg.Any<CancellationToken>()).Returns((int?)null);

        var exception = await Assert.ThrowsAsync<DomainException>(
            () => _service.AdjustAsync(_doubles.Admin.AuthUserId, _expertId, new AdminCreditAdjustDto(-5, "Mükerrer yükleme"), CancellationToken.None));

        Assert.Equal(ErrorKeys.CreditBalanceNegative, exception.ErrorKey);
        Assert.Empty(_ledgerRows);
        Assert.Empty(_doubles.AddedAuditLogs);
        await _transaction.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
        await _doubles.UnitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Adjust_writes_ledger_and_audit_in_one_committed_transaction()
    {
        _credits.TryApplyDeltaAsync(_expertId, 20, Arg.Any<CancellationToken>()).Returns(20);
        _credits.GetBalanceAsync(_expertId, Arg.Any<CancellationToken>()).Returns(20);

        var result = await _service.AdjustAsync(
            _doubles.Admin.AuthUserId,
            _expertId,
            new AdminCreditAdjustDto(20, "  Demo bakiyesi  "),
            CancellationToken.None);

        var row = Assert.Single(_ledgerRows);
        Assert.Equal(CreditTransactionTypes.AdminAdjust, row.Type);
        Assert.Equal(20, row.Amount);
        Assert.Equal(20, row.BalanceAfter);
        Assert.Equal(_doubles.Admin.Id, row.AdminUserId);
        Assert.Equal("Demo bakiyesi", row.Reason);

        var audit = Assert.Single(_doubles.AddedAuditLogs);
        Assert.Equal(AuditActions.CreditAdjust, audit.Action);
        Assert.Equal(_expertId.ToString(), audit.EntityId);
        Assert.Equal("Demo bakiyesi", audit.Reason);
        Assert.Contains("\"balance\":0", audit.OldValue, StringComparison.Ordinal);
        Assert.Contains("\"balance\":20", audit.NewValue, StringComparison.Ordinal);

        await _transaction.Received(1).CommitAsync(Arg.Any<CancellationToken>());
        Assert.Equal(20, result.Balance);
        Assert.Equal("Elif Kaya", result.FullName);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("   ")]
    public async Task Adjust_requires_a_reason(string? reason)
    {
        var exception = await Assert.ThrowsAsync<DomainException>(
            () => _service.AdjustAsync(_doubles.Admin.AuthUserId, _expertId, new AdminCreditAdjustDto(5, reason), CancellationToken.None));

        Assert.Equal(ErrorKeys.CreditAdjustReasonRequired, exception.ErrorKey);
        await _credits.DidNotReceiveWithAnyArgs().TryApplyDeltaAsync(default, default, default);
    }

    [Fact]
    public async Task Adjust_of_zero_is_rejected()
    {
        var exception = await Assert.ThrowsAsync<DomainException>(
            () => _service.AdjustAsync(_doubles.Admin.AuthUserId, _expertId, new AdminCreditAdjustDto(0, "gerekçe"), CancellationToken.None));

        Assert.Equal(ErrorKeys.ValidationFailed, exception.ErrorKey);
    }

    [Fact]
    public async Task Adjust_for_unknown_expert_is_not_found()
    {
        var exception = await Assert.ThrowsAsync<DomainException>(
            () => _service.AdjustAsync(_doubles.Admin.AuthUserId, Guid.NewGuid(), new AdminCreditAdjustDto(5, "gerekçe"), CancellationToken.None));

        Assert.Equal(ErrorKeys.ExpertNotFound, exception.ErrorKey);
    }

    [Fact]
    public async Task Package_with_purchase_history_cannot_be_deleted()
    {
        var package = new CreditPackage { Id = Guid.NewGuid(), Name = "Başlangıç", Credits = 20, PriceTry = 500m };
        _packages.GetByIdAsync(package.Id, Arg.Any<CancellationToken>()).Returns(package);
        _payments.AnyForPackageAsync(package.Id, Arg.Any<CancellationToken>()).Returns(true);

        var exception = await Assert.ThrowsAsync<DomainException>(
            () => _service.DeletePackageAsync(_doubles.Admin.AuthUserId, package.Id, CancellationToken.None));

        Assert.Equal(ErrorKeys.CreditPackageInUse, exception.ErrorKey);
        _packages.DidNotReceive().Delete(Arg.Any<CreditPackage>());
    }

    [Fact]
    public async Task Package_create_is_audited()
    {
        var created = await _service.CreatePackageAsync(
            _doubles.Admin.AuthUserId,
            new CreateAdminCreditPackageDto("  Başlangıç ", 20, 500m, true, 1),
            CancellationToken.None);

        Assert.Equal("Başlangıç", created.Name);
        Assert.Equal(AuditActions.CreditPackageCreate, Assert.Single(_doubles.AddedAuditLogs).Action);
        await _packages.Received(1).AddAsync(Arg.Is<CreditPackage>(package => package.Credits == 20), Arg.Any<CancellationToken>());
    }

    public void Dispose() => _database.Dispose();
}
