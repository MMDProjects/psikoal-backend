using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PsikoAl.Common.Dtos.Credit;
using PsikoAl.Common.Dtos.Payment;
using PsikoAl.Services.Abstractions;

namespace PsikoAl.Api.ContractTests;

// Sözleşme: kredi-sozlesme (2026-10-14) ve docs/PAYMENTS.md. Mobil uygulama ve ileride web
// sitesi bu alan adlarına göre yazılacak; servis sahte, JSON'u üreten pipeline gerçek
// (controller, { data } zarfı, camelCase, kimlik doğrulama).
public sealed class CreditContractTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private static readonly Guid UserId = Guid.NewGuid();

    [Fact]
    public async Task Expert_credit_endpoints_require_authentication()
    {
        using var client = factory.CreateClient();

        var me = await client.GetAsync("/credits/me", CancellationToken.None);
        var transactions = await client.GetAsync("/credits/transactions", CancellationToken.None);
        var init = await client.PostAsJsonAsync("/payments/paytr/init", new { packageId = Guid.NewGuid() }, CancellationToken.None);

        Assert.Equal(HttpStatusCode.Unauthorized, me.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, transactions.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, init.StatusCode);
    }

    [Fact]
    public async Task Admin_credit_endpoints_require_authentication()
    {
        using var client = factory.CreateClient();
        var id = Guid.NewGuid();

        var responses = new[]
        {
            await client.GetAsync("/admin/credits/packages", CancellationToken.None),
            await client.PostAsJsonAsync("/admin/credits/packages", new { name = "p", credits = 1, priceTry = 1m }, CancellationToken.None),
            await client.PutAsJsonAsync($"/admin/credits/packages/{id}", new { name = "p", credits = 1, priceTry = 1m }, CancellationToken.None),
            await client.DeleteAsync($"/admin/credits/packages/{id}", CancellationToken.None),
            await client.GetAsync("/admin/credits/payments?status=PAID", CancellationToken.None),
            await client.GetAsync($"/admin/credits/experts/{id}", CancellationToken.None),
            await client.PostAsJsonAsync($"/admin/credits/experts/{id}/adjust", new { amount = 5, reason = "r" }, CancellationToken.None),
        };

        Assert.All(responses, response => Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode));
    }

    [Fact]
    public async Task Packages_are_public_and_match_the_web_contract()
    {
        using var client = CreateClient(new FakeCreditService(), new FakePaymentService(), authenticated: false);

        var response = await client.GetAsync("/credits/packages", CancellationToken.None);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(CancellationToken.None);
        var package = Assert.Single(body.GetProperty("data").EnumerateArray());
        Assert.Equal(JsonValueKind.String, package.GetProperty("id").ValueKind);
        Assert.Equal("Başlangıç", package.GetProperty("name").GetString());
        Assert.Equal(20, package.GetProperty("credits").GetInt32());
        Assert.Equal(500m, package.GetProperty("priceTry").GetDecimal());
        Assert.Equal("500,00 ₺", package.GetProperty("priceLabel").GetString());
        Assert.Equal("25,00 ₺ / kredi", package.GetProperty("unitPriceLabel").GetString());
    }

    [Fact]
    public async Task Balance_matches_the_mobile_contract()
    {
        using var client = CreateClient(new FakeCreditService(), new FakePaymentService(), authenticated: true);

        var body = await client.GetFromJsonAsync<JsonElement>("/credits/me", CancellationToken.None);

        var data = body.GetProperty("data");
        Assert.Equal(12, data.GetProperty("balance").GetInt32());
        Assert.Equal(1, data.GetProperty("offerCost").GetInt32());
        Assert.Equal("12 kredi", data.GetProperty("balanceLabel").GetString());
    }

    [Fact]
    public async Task Transactions_match_the_mobile_contract_and_forward_paging()
    {
        var credits = new FakeCreditService();
        using var client = CreateClient(credits, new FakePaymentService(), authenticated: true);

        var body = await client.GetFromJsonAsync<JsonElement>("/credits/transactions?page=2&perPage=10", CancellationToken.None);

        Assert.Equal((2, 10), credits.LastPaging);
        var meta = body.GetProperty("meta");
        Assert.Equal(2, meta.GetProperty("page").GetInt32());
        Assert.Equal(11, meta.GetProperty("total").GetInt32());
        Assert.Equal(10, meta.GetProperty("perPage").GetInt32());

        var item = Assert.Single(body.GetProperty("data").EnumerateArray());
        foreach (var field in (string[])["id", "type", "typeLabel", "description", "createdAt", "timeLabel"])
        {
            Assert.Equal(JsonValueKind.String, item.GetProperty(field).ValueKind);
        }

        Assert.Equal(-1, item.GetProperty("amount").GetInt32());
        Assert.Equal(11, item.GetProperty("balanceAfter").GetInt32());
        Assert.Equal("OFFER_SPEND", item.GetProperty("type").GetString());
        Assert.True(item.TryGetProperty("offerId", out var offerId), "offerId alanı (null olsa da) bulunmalı.");
        Assert.Equal(JsonValueKind.String, offerId.ValueKind);
    }

    [Fact]
    public async Task Payment_init_returns_iframe_data()
    {
        var payments = new FakePaymentService();
        using var client = CreateClient(new FakeCreditService(), payments, authenticated: true);
        var packageId = Guid.NewGuid();

        var response = await client.PostAsJsonAsync("/payments/paytr/init", new { packageId }, CancellationToken.None);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(packageId, payments.LastPackageId);
        var data = (await response.Content.ReadFromJsonAsync<JsonElement>(CancellationToken.None)).GetProperty("data");
        Assert.Equal("tok123", data.GetProperty("iframeToken").GetString());
        Assert.Equal("PA0123ABC", data.GetProperty("merchantOid").GetString());
        Assert.Equal("https://www.paytr.com/odeme/guvenli/tok123", data.GetProperty("iframeUrl").GetString());
    }

    [Fact]
    public async Task Payment_init_rejects_an_empty_package_id()
    {
        using var client = CreateClient(new FakeCreditService(), new FakePaymentService(), authenticated: true);

        var response = await client.PostAsJsonAsync("/payments/paytr/init", new { packageId = Guid.Empty }, CancellationToken.None);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData(PayTrCallbackOutcome.Paid)]
    [InlineData(PayTrCallbackOutcome.Failed)]
    [InlineData(PayTrCallbackOutcome.AlreadyProcessed)]
    public async Task Callback_is_anonymous_form_post_and_answers_plain_OK(PayTrCallbackOutcome outcome)
    {
        var payments = new FakePaymentService { Outcome = outcome };
        using var client = CreateClient(new FakeCreditService(), payments, authenticated: false);

        var response = await client.PostAsync("/payments/paytr/callback", CallbackForm(), CancellationToken.None);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/plain", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal("OK", await response.Content.ReadAsStringAsync(CancellationToken.None));
        Assert.Equal("PA0123ABC", payments.LastCallback?.MerchantOid);
        Assert.Equal("success", payments.LastCallback?.Status);
        Assert.Equal("50000", payments.LastCallback?.TotalAmount);
        Assert.Equal("card", payments.LastCallback?.PaymentType);
    }

    [Theory]
    [InlineData(PayTrCallbackOutcome.InvalidHash)]
    [InlineData(PayTrCallbackOutcome.UnknownOrder)]
    public async Task Callback_does_not_answer_OK_when_the_notification_is_rejected(PayTrCallbackOutcome outcome)
    {
        using var client = CreateClient(new FakeCreditService(), new FakePaymentService { Outcome = outcome }, authenticated: false);

        var response = await client.PostAsync("/payments/paytr/callback", CallbackForm(), CancellationToken.None);

        Assert.False(response.IsSuccessStatusCode);
        Assert.NotEqual("OK", await response.Content.ReadAsStringAsync(CancellationToken.None));
    }

    [Fact]
    public async Task Callback_with_missing_fields_is_rejected_before_the_service()
    {
        var payments = new FakePaymentService();
        using var client = CreateClient(new FakeCreditService(), payments, authenticated: false);

        var response = await client.PostAsync(
            "/payments/paytr/callback",
            new FormUrlEncodedContent(new Dictionary<string, string> { ["merchant_oid"] = "PA1" }),
            CancellationToken.None);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Null(payments.LastCallback);
    }

    private static FormUrlEncodedContent CallbackForm()
        => new(new Dictionary<string, string>
        {
            ["merchant_oid"] = "PA0123ABC",
            ["status"] = "success",
            ["total_amount"] = "50000",
            ["hash"] = "aGFzaA==",
            ["payment_type"] = "card",
            ["currency"] = "TL",
            ["payment_amount"] = "50000",
            ["test_mode"] = "1",
        });

    private HttpClient CreateClient(ICreditService credits, ICreditPaymentService payments, bool authenticated)
    {
        var client = factory
            .WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
            {
                services.AddScoped(_ => credits);
                services.AddScoped(_ => payments);
                services.AddScoped<IAccountStatusService, ActiveAccountStatus>();
                services
                    .AddAuthentication(TestAuthHandler.SchemeName)
                    .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });
            }))
            .CreateClient();

        if (authenticated)
        {
            client.DefaultRequestHeaders.Add(TestAuthHandler.HeaderName, UserId.ToString());
        }

        return client;
    }

    private sealed class TestAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        public const string SchemeName = "Test";
        public const string HeaderName = "X-Test-User";

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Request.Headers.TryGetValue(HeaderName, out var userId))
            {
                return Task.FromResult(AuthenticateResult.NoResult());
            }

            var identity = new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, userId.ToString())], SchemeName);
            return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName)));
        }
    }

    private sealed class ActiveAccountStatus : IAccountStatusService
    {
        public Task<bool> IsActiveAsync(Guid userId, CancellationToken cancellationToken) => Task.FromResult(true);

        public void Invalidate(Guid userId)
        {
        }
    }

    private sealed class FakeCreditService : ICreditService
    {
        public (int? Page, int? PerPage) LastPaging { get; private set; }

        public Task<CreditBalanceDto> GetMyBalanceAsync(Guid expertUserId, CancellationToken cancellationToken)
            => Task.FromResult(new CreditBalanceDto(12, 1, "12 kredi"));

        public Task<CreditTransactionListResult> ListMyTransactionsAsync(
            Guid expertUserId,
            int? page,
            int? perPage,
            CancellationToken cancellationToken)
        {
            LastPaging = (page, perPage);
            var item = new CreditTransactionDto(
                Guid.NewGuid(),
                -1,
                11,
                "OFFER_SPEND",
                "Teklif gönderimi",
                "İlana teklif gönderildi",
                Guid.NewGuid(),
                DateTimeOffset.UtcNow,
                "şimdi");
            return Task.FromResult(new CreditTransactionListResult([item], page ?? 1, perPage ?? 20, 11));
        }

        public Task<IReadOnlyList<CreditPackageDto>> ListActivePackagesAsync(CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<CreditPackageDto>>(
                [new CreditPackageDto(Guid.NewGuid(), "Başlangıç", 20, 500m, "500,00 ₺", "25,00 ₺ / kredi")]);
    }

    private sealed class FakePaymentService : ICreditPaymentService
    {
        public PayTrCallbackOutcome Outcome { get; init; } = PayTrCallbackOutcome.Paid;

        public Guid? LastPackageId { get; private set; }

        public PayTrCallbackDto? LastCallback { get; private set; }

        public Task<PayTrInitResultDto> InitPayTrAsync(
            Guid expertUserId,
            Guid packageId,
            string userIp,
            CancellationToken cancellationToken)
        {
            LastPackageId = packageId;
            return Task.FromResult(new PayTrInitResultDto("tok123", "PA0123ABC", "https://www.paytr.com/odeme/guvenli/tok123"));
        }

        public Task<PayTrCallbackOutcome> HandlePayTrCallbackAsync(PayTrCallbackDto callback, CancellationToken cancellationToken)
        {
            LastCallback = callback;
            return Task.FromResult(Outcome);
        }
    }
}
