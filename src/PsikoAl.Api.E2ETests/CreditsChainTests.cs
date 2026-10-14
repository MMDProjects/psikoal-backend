using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Npgsql;
using PsikoAl.Services.Payments;

namespace PsikoAl.Api.E2ETests;

/// <summary>
/// Uzman kredisi (Armut modeli) zinciri: admin kredi verir → uzman teklif gönderir →
/// kredi düşer → defter ve bakiye mobil uçlarında görünür; PayTR bildirimi krediyi bir kez yükler.
/// </summary>
/// <remarks>
/// Birim testleri servis sözleşmesini ölçer; burada ölçülen VERİTABANI yarısıdır:
/// koşullu UPDATE'in satır kilidi altında eşzamanlı iki teklifi sıraya sokması,
/// teklif yazılamazsa düşümün geri alınması ve CHECK (credit_balance >= 0).
/// get-token ucu bilerek çağrılmaz (gerçek PayTR'ye gider); ödeme satırı SQL ile
/// PENDING açılır ve bildirim, E2EFactory'deki sahte mağaza anahtarıyla imzalanır.
/// <c>Category=E2E</c>: CI birim adımı bu projeyi dışlar.
/// </remarks>
[Trait("Category", "E2E")]
[Collection(E2ECollection.Name)]
public sealed class CreditsChainTests(E2EFixture fixture) : IAsyncLifetime
{
    private const string ExpertEmail = "e2e-credit-expert@psikoal.test";
    private const string ExpertPassword = "E2eCreditExpertPass123!";

    private static Task<AuthenticatedUser>? _sharedExpert;

    private readonly List<Guid> _listingIds = [];
    private readonly List<Guid> _packageIds = [];
    private Guid _expertId;
    private string _expertToken = string.Empty;
    private string _specialization = string.Empty;

    public async Task InitializeAsync()
    {
        _specialization = await CreateSpecializationCategoryAsync();

        var expert = await (_sharedExpert ??= SupabaseTestIdentity.EnsureUserAsync(ExpertEmail, ExpertPassword, CancellationToken.None));
        _expertId = expert.AuthUserId;
        _expertToken = expert.AccessToken;

        // Bakiye her testte SIFIRDAN kurulur: defter append-only olduğu için satırlar
        // silinmez, ama bakiye doğrudan 0'a çekilir — testler mutlak değer ölçer.
        await ExecuteSqlAsync(
            """
            update public.profiles set role = 'expert', first_name = 'E2E', last_name = 'Kredi' where id = @id;
            insert into public.experts (id, title, specializations, status, credit_balance)
            values (@id, 'E2E Klinik Psikolog', array[@spec], 'approved', 0)
            on conflict (id) do update
              set status = 'approved', specializations = array[@spec], credit_balance = 0;
            """,
            _expertId,
            _specialization);
    }

    public async Task DisposeAsync()
    {
        foreach (var listingId in _listingIds)
        {
            await ExecuteSqlAsync(
                """
                delete from public.offers where listing_id = @id;
                delete from public.listings where id = @id;
                """,
                listingId);
        }

        foreach (var packageId in _packageIds)
        {
            await ExecuteSqlAsync(
                """
                delete from public.credit_transactions where payment_id in (select id from public.credit_payments where package_id = @id);
                delete from public.credit_payments where package_id = @id;
                delete from public.credit_packages where id = @id;
                """,
                packageId);
        }
    }

    [Fact]
    public async Task Admin_kredisi_ile_gönderilen_teklif_bir_kredi_düşer_ve_defterde_görünür()
    {
        await AdminAdjustAsync(3, "E2E başlangıç bakiyesi");
        var listingId = await CreateOpenListingAsync();

        var offer = await CreateOfferAsync(listingId);
        Assert.Equal(HttpStatusCode.OK, offer.StatusCode);
        var offerId = (await offer.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        var balance = await GetBalanceAsync();
        Assert.Equal(2, balance.GetProperty("balance").GetInt32());
        Assert.Equal(1, balance.GetProperty("offerCost").GetInt32());
        Assert.Equal("2 kredi", balance.GetProperty("balanceLabel").GetString());

        var latest = (await ListTransactionsAsync()).First();
        Assert.Equal("OFFER_SPEND", latest.GetProperty("type").GetString());
        Assert.Equal(-1, latest.GetProperty("amount").GetInt32());
        Assert.Equal(2, latest.GetProperty("balanceAfter").GetInt32());
        Assert.Equal(offerId, latest.GetProperty("offerId").GetGuid());
    }

    [Fact]
    public async Task Bakiye_yetmezse_422_INSUFFICIENT_CREDITS_ve_teklif_yazılmaz()
    {
        var listingId = await CreateOpenListingAsync();

        var response = await CreateOfferAsync(listingId);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("INSUFFICIENT_CREDITS", body.GetProperty("code").GetString());
        Assert.Equal("Teklif göndermek için yeterli krediniz yok.", body.GetProperty("message").GetString());
        Assert.Equal(0, await CountOffersAsync(listingId));
        Assert.Equal(0, (await GetBalanceAsync()).GetProperty("balance").GetInt32());
    }

    [Fact]
    public async Task Aynı_ilana_ikinci_teklif_kredi_düşmez()
    {
        await AdminAdjustAsync(5, "E2E bakiye");
        var listingId = await CreateOpenListingAsync();
        (await CreateOfferAsync(listingId)).EnsureSuccessStatusCode();

        var second = await CreateOfferAsync(listingId);

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
        Assert.Equal(4, (await GetBalanceAsync()).GetProperty("balance").GetInt32());
    }

    [Fact]
    public async Task Tek_kredi_ile_eşzamanlı_iki_teklif_yalnız_birini_geçirir()
    {
        await AdminAdjustAsync(1, "E2E tek kredi");
        var first = await CreateOpenListingAsync();
        var second = await CreateOpenListingAsync();

        var responses = await Task.WhenAll(CreateOfferAsync(first), CreateOfferAsync(second));

        Assert.Single(responses, response => response.StatusCode == HttpStatusCode.OK);
        Assert.Single(responses, response => response.StatusCode == HttpStatusCode.UnprocessableEntity);
        Assert.Equal(0, (await GetBalanceAsync()).GetProperty("balance").GetInt32());
        Assert.Equal(1, await CountOffersAsync(first) + await CountOffersAsync(second));
    }

    [Fact]
    public async Task Admin_düzeltmesi_bakiyeyi_eksiye_düşüremez()
    {
        await AdminAdjustAsync(2, "E2E bakiye");

        var response = await fixture.Admin.PostAsJsonAsync(
            $"/admin/credits/experts/{_expertId}/adjust",
            new { amount = -3, reason = "E2E fazla düşüm" });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("CREDIT_BALANCE_NEGATIVE", body.GetProperty("code").GetString());
        Assert.Equal(2, (await GetBalanceAsync()).GetProperty("balance").GetInt32());
    }

    [Fact]
    public async Task PayTR_bildirimi_krediyi_bir_kez_yükler_ve_tekrarına_yine_OK_der()
    {
        var packageId = await CreatePackageAsync(credits: 20, priceTry: 500m);
        var merchantOid = await InsertPendingPaymentAsync(packageId, credits: 20, amountTry: 500m);

        var first = await PostCallbackAsync(merchantOid, "success", "50000");
        var repeat = await PostCallbackAsync(merchantOid, "success", "50000");

        Assert.Equal("OK", await first.Content.ReadAsStringAsync());
        Assert.Equal("OK", await repeat.Content.ReadAsStringAsync());
        Assert.Equal(20, (await GetBalanceAsync()).GetProperty("balance").GetInt32());
        var purchase = (await ListTransactionsAsync()).First();
        Assert.Equal("PURCHASE", purchase.GetProperty("type").GetString());
        Assert.Equal(20, purchase.GetProperty("amount").GetInt32());
    }

    [Fact]
    public async Task Hash_i_bozuk_bildirim_reddedilir_ve_kredi_yüklemez()
    {
        var packageId = await CreatePackageAsync(credits: 10, priceTry: 300m);
        var merchantOid = await InsertPendingPaymentAsync(packageId, credits: 10, amountTry: 300m);

        var response = await PostCallbackAsync(merchantOid, "success", "30000", hashOverride: "c2FodGVoYXNo");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.NotEqual("OK", await response.Content.ReadAsStringAsync());
        Assert.Equal(0, (await GetBalanceAsync()).GetProperty("balance").GetInt32());
    }

    [Fact]
    public async Task Başarısız_ödeme_bildirimi_kredi_yüklemez()
    {
        var packageId = await CreatePackageAsync(credits: 10, priceTry: 300m);
        var merchantOid = await InsertPendingPaymentAsync(packageId, credits: 10, amountTry: 300m);

        var response = await PostCallbackAsync(merchantOid, "failed", "30000");

        Assert.Equal("OK", await response.Content.ReadAsStringAsync());
        Assert.Equal(0, (await GetBalanceAsync()).GetProperty("balance").GetInt32());
        var status = await (await fixture.Admin.GetAsync("/admin/credits/payments?status=FAILED"))
            .Content.ReadFromJsonAsync<JsonElement>();
        Assert.Contains(
            status.GetProperty("data").EnumerateArray(),
            row => row.GetProperty("merchantOid").GetString() == merchantOid);
    }

    [Fact]
    public async Task Paket_listesi_anonim_ve_yalnız_aktif_paketleri_döner()
    {
        var active = await CreatePackageAsync(credits: 50, priceTry: 1000m);
        var inactive = await CreatePackageAsync(credits: 5, priceTry: 150m, isActive: false);

        var body = await fixture.Mobile.GetFromJsonAsync<JsonElement>("/credits/packages");

        var ids = body.GetProperty("data").EnumerateArray().Select(row => row.GetProperty("id").GetGuid()).ToList();
        Assert.Contains(active, ids);
        Assert.DoesNotContain(inactive, ids);
    }

    // ------------------------------------------------------------------ yardımcılar

    private async Task AdminAdjustAsync(int amount, string reason)
    {
        var response = await fixture.Admin.PostAsJsonAsync(
            $"/admin/credits/experts/{_expertId}/adjust",
            new { amount, reason });
        if (response.StatusCode != HttpStatusCode.OK)
        {
            throw new InvalidOperationException(
                $"Kredi düzeltmesi başarısız ({(int)response.StatusCode}): {await response.Content.ReadAsStringAsync()}");
        }
    }

    private async Task<JsonElement> GetBalanceAsync()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/credits/me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _expertToken);
        var response = await fixture.Mobile.SendAsync(request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("data");
    }

    private async Task<List<JsonElement>> ListTransactionsAsync()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/credits/transactions?perPage=10");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _expertToken);
        var response = await fixture.Mobile.SendAsync(request);
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        return [.. body.GetProperty("data").EnumerateArray()];
    }

    private async Task<HttpResponseMessage> CreateOfferAsync(Guid listingId)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/offers")
        {
            Content = JsonContent.Create(new
            {
                listingId,
                title = "E2E kredi teklifi",
                price = 1000m,
                sessionType = "online",
                description = (string?)null,
            }),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _expertToken);
        return await fixture.Mobile.SendAsync(request);
    }

    private async Task<Guid> CreatePackageAsync(int credits, decimal priceTry, bool isActive = true)
    {
        var response = await fixture.Admin.PostAsJsonAsync(
            "/admin/credits/packages",
            new { name = $"E2E {credits} kredi", credits, priceTry, isActive, sortOrder = 900 });
        response.EnsureSuccessStatusCode();
        var id = (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        _packageIds.Add(id);
        return id;
    }

    private async Task<string> InsertPendingPaymentAsync(Guid packageId, int credits, decimal amountTry)
    {
        var merchantOid = "PAE2E" + Guid.NewGuid().ToString("N").ToUpperInvariant();
        await using var dataSource = new NpgsqlDataSourceBuilder(LocalStack.PostgresConnectionString).Build();
        await using var connection = await dataSource.OpenConnectionAsync(CancellationToken.None);
        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            insert into public.credit_payments (expert_id, package_id, merchant_oid, amount_try, credits, status)
            values (@expert, @package, @oid, @amount, @credits, 'PENDING');
            """;
        command.Parameters.AddWithValue("expert", _expertId);
        command.Parameters.AddWithValue("package", packageId);
        command.Parameters.AddWithValue("oid", merchantOid);
        command.Parameters.AddWithValue("amount", amountTry);
        command.Parameters.AddWithValue("credits", credits);
        await command.ExecuteNonQueryAsync(CancellationToken.None);
        return merchantOid;
    }

    private Task<HttpResponseMessage> PostCallbackAsync(
        string merchantOid,
        string status,
        string totalAmount,
        string? hashOverride = null)
    {
        var hash = hashOverride ?? PayTrHash.CallbackHash(
            merchantOid, status, totalAmount, E2EFactory.PayTrMerchantSalt, E2EFactory.PayTrMerchantKey);

        return fixture.Mobile.PostAsync(
            "/payments/paytr/callback",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["merchant_oid"] = merchantOid,
                ["status"] = status,
                ["total_amount"] = totalAmount,
                ["hash"] = hash,
                ["payment_type"] = "card",
                ["currency"] = "TL",
                ["payment_amount"] = totalAmount,
                ["test_mode"] = "1",
            }));
    }

    private async Task<Guid> CreateOpenListingAsync()
    {
        var response = await fixture.Client.PostAsJsonAsync(
            "/listings",
            new
            {
                title = $"E2E kredi ilanı {Guid.NewGuid():N}",
                description = "E2E kredi zinciri testinin ürettiği geçici ilan.",
                specialization = new[] { _specialization },
                budgetMin = 750m,
                budgetMax = 1500m,
                preferredSessionType = "online",
                city = "İstanbul",
                assessmentResultId = (Guid?)null,
            });
        if (response.StatusCode != HttpStatusCode.OK)
        {
            throw new InvalidOperationException(
                $"İlan oluşturulamadı ({(int)response.StatusCode}): {await response.Content.ReadAsStringAsync()}");
        }

        var listingId = (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        _listingIds.Add(listingId);

        (await fixture.Admin.PostAsync($"/admin/listings/{listingId}/approve", null)).EnsureSuccessStatusCode();
        return listingId;
    }

    private async Task<string> CreateSpecializationCategoryAsync()
    {
        var slug = fixture.NewCategorySlug();
        var name = $"E2E {slug}";
        var response = await fixture.Admin.PostAsJsonAsync(
            "/admin/categories",
            new
            {
                slug,
                name,
                icon = "brain",
                summary = "E2E kredi testinin ürettiği geçici kategori.",
                description = "Bu kayıt otomatik testler tarafından üretilir ve silinir.",
                blogTag = slug,
                assessmentCategory = (string?)null,
            });
        response.EnsureSuccessStatusCode();
        return name;
    }

    private static async Task<int> CountOffersAsync(Guid listingId)
    {
        await using var dataSource = new NpgsqlDataSourceBuilder(LocalStack.PostgresConnectionString).Build();
        await using var connection = await dataSource.OpenConnectionAsync(CancellationToken.None);
        await using var command = connection.CreateCommand();
        command.CommandText = "select count(*) from public.offers where listing_id = @id;";
        command.Parameters.AddWithValue("id", listingId);
        return Convert.ToInt32(await command.ExecuteScalarAsync(CancellationToken.None), System.Globalization.CultureInfo.InvariantCulture);
    }

    private static async Task ExecuteSqlAsync(string sql, Guid id, string? spec = null)
    {
        await using var dataSource = new NpgsqlDataSourceBuilder(LocalStack.PostgresConnectionString).Build();
        await using var connection = await dataSource.OpenConnectionAsync(CancellationToken.None);
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddWithValue("id", id);
        if (spec is not null)
        {
            command.Parameters.AddWithValue("spec", spec);
        }

        await command.ExecuteNonQueryAsync(CancellationToken.None);
    }
}
