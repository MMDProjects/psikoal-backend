using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Npgsql;

namespace PsikoAl.Api.E2ETests;

/// <summary>
/// "Admin bir yorumu onaylayınca mobilde görünüyor mu, reddedince gerçekten gizleniyor mu?"
/// sorusunun makine cevabı.
/// </summary>
/// <remarks>
/// Zincir: danışan token'ı -> POST /experts/{id}/reviews (pending) -> admin token'ı ->
/// POST /admin/reviews/{id}/approve -> Postgres -> mobil uygulamanın çağırdığı
/// GET /experts/{id}/reviews + GET /experts/{id}.
///
/// Yorum moderasyonu kategoriden daha derin bir zincir ve bilinçli seçildi: aynı
/// <c>reviews</c> tablosu mobil tarafa İKİ AYRI yoldan okunuyor —
/// yorum listesi <c>ReviewService.ListApprovedForExpertAsync</c> (EF sorgusu,
/// status == 'approved'), puan/adet ise <c>expert_ratings</c> VIEW'u
/// (SQL tarafında status = 'approved' filtresi). Biri çalışıp diğeri kırılabilir;
/// bu yüzden her senaryoda İKİSİ birden doğrulanıyor.
/// </remarks>
/// <remarks>
/// <c>Category=E2E</c> trait'i şart: <c>ci.yml</c>'in birim test adımı
/// <c>--filter "Category!=E2E"</c> ile bu sınıfı dışlar (orada lokal Supabase yok),
/// <c>e2e.yml</c> ise yalnız bunları koşar.
/// </remarks>
[Trait("Category", "E2E")]
[Collection(E2ECollection.Name)]
public sealed class ReviewsChainTests(E2EFixture fixture)
{
    // Uzman hesabı hiçbir zaman giriş yapmıyor (yorumu danışan yazıyor, uzman tarafı
    // tamamen okuma), ama GoTrue admin API'si şifresiz kullanıcı kabul etmiyor.
    private const string ExpertPassword = "E2eExpertPass123!";

    private const string ReviewComment = "E2E moderasyon zincirinin ürettiği geçici yorum.";

    /// <summary>Ana zincir: onaylanan yorum hem listede hem PUANDA görünür.</summary>
    /// <remarks>
    /// Puanın da değiştiğini doğrulamak şart. Yorum listesi ile rating iki AYRI okuma
    /// yolundan geliyor (bkz. sınıf açıklaması); yalnız listeye bakan bir test,
    /// <c>expert_ratings</c> view'u bozulduğunda da yeşil kalırdı — mobil ekranın
    /// üstündeki yıldız 0.0 gösterirken yorumlar aşağıda listeleniyor olurdu.
    /// </remarks>
    [Fact]
    public Task Admin_onayladığı_yorum_mobil_uzman_profilinde_ve_puanında_görünür()
        => RunSeededAsync(matchCount: 1, expertStatus: "approved", async seed =>
        {
            // Eklemeden ÖNCE yok olduğunu doğrula. Bu iki satır olmadan test, uzmanın
            // yorumu zaten var olduğu için de yeşil olurdu - yani hiçbir şey kanıtlamazdı.
            Assert.Empty(await FetchMobileReviewsAsync(seed.ExpertId));
            var (ratingBefore, countBefore) = await FetchMobileRatingAsync(seed.ExpertId);
            Assert.Equal(0d, ratingBefore);
            Assert.Equal(0, countBefore);

            var reviewId = await CreateReviewAsync(seed.ExpertId, seed.Chains[0].MatchId, rating: 4, sessionType: "online");

            var approve = await fixture.Admin.PostAsync($"/admin/reviews/{reviewId}/approve", null);
            Assert.Equal(HttpStatusCode.OK, approve.StatusCode);

            var reviews = await FetchMobileReviewsAsync(seed.ExpertId);
            var review = reviews.Single(item => item.GetProperty("id").GetGuid() == reviewId);

            // Sadece "listede var" yetmez: mobil ekran yorumun METNİNİ ve puanını çiziyor.
            Assert.Equal(ReviewComment, review.GetProperty("comment").GetString());
            Assert.Equal(4, review.GetProperty("rating").GetInt32());

            var (ratingAfter, countAfter) = await FetchMobileRatingAsync(seed.ExpertId);
            Assert.Equal(4d, ratingAfter);
            Assert.Equal(1, countAfter);
        });

    /// <summary>
    /// Onay bekleyen yorum mobilde yok; ama admin panelinde VAR.
    /// </summary>
    /// <remarks>
    /// "Aynı tablo, farklı filtre" kırılma noktasının ta kendisi:
    /// <c>AdminReviewService.ListAsync</c> status boş gelirse HİÇBİR filtre uygulamaz,
    /// <c>ReviewService.ListApprovedForExpertAsync</c> ise status == 'approved' zorunlu
    /// tutar. Ayrıca <c>ReviewDto</c>'da <c>status</c> alanı YOK — mobil bir yorumun
    /// onaylı olup olmadığını gövdeden anlayamaz, tamamen sunucu filtresine güvenir.
    /// O filtre bir gün düşse mobil bekleyen yorumları sorgusuzca çizer ve hiçbir
    /// şema/parse hatası oluşmaz. Bu boşluğu yalnız bu test kapatıyor.
    /// </remarks>
    [Fact]
    public Task Onay_bekleyen_yorum_mobil_uzman_profilinde_görünmez_ve_puana_girmez()
        => RunSeededAsync(matchCount: 1, expertStatus: "approved", async seed =>
        {
            var reviewId = await CreateReviewAsync(seed.ExpertId, seed.Chains[0].MatchId, rating: 5, sessionType: "online");

            // Yorum GERÇEKTEN üretildi mi? Kanıtı admin ucu: moderasyon kuyruğunda pending duruyor.
            // Bu doğrulama olmadan "mobilde görünmüyor" iddiası, yorum hiç oluşmadığı için de
            // yeşil olurdu.
            var pending = await FetchAdminReviewsAsync("pending");
            var queued = pending.Single(item => item.GetProperty("id").GetGuid() == reviewId);
            Assert.Equal("pending", queued.GetProperty("status").GetString());

            // Filtresiz çağrı: status parametresi boşken hiçbir filtre uygulanmamalı.
            var all = await FetchAdminReviewsAsync(status: null);
            Assert.Contains(all, item => item.GetProperty("id").GetGuid() == reviewId);

            Assert.Empty(await FetchMobileReviewsAsync(seed.ExpertId));

            var (rating, count) = await FetchMobileRatingAsync(seed.ExpertId);
            Assert.Equal(0d, rating);
            Assert.Equal(0, count);
        });

    /// <summary>Ters yön: reddedilen yorum ne listeye ne ortalamaya girer.</summary>
    /// <remarks>
    /// İki ayrı match üzerinden iki yorum kuruluyor; biri onaylanıyor (5), biri
    /// reddediliyor (1). Tek yorumla çalışan bir test "reddedince kayboldu"yu ölçer
    /// ama ORTALAMAYI ölçemez: 1 puanlık reddedilen yorum ortalamaya sızsaydı rating
    /// 5.0 değil 3.0 olurdu ve tek yorumlu bir kurulumda bu fark hiç görünmezdi.
    ///
    /// Sebepsiz red ayrıca 422 dönmeli: red gerekçesi danışana giden REVIEW_REJECTED
    /// bildiriminin metnine gömülüyor ({reason} yer tutucusu, notification_templates),
    /// yani boş sebep sessizce geçerse danışan "reddedildi: " diye yarım bir metin alır.
    /// </remarks>
    [Fact]
    public Task Admin_reddettiği_yorum_mobil_uzman_profilinde_görünmez_ve_puanı_düşürmez()
        => RunSeededAsync(matchCount: 2, expertStatus: "approved", async seed =>
        {
            var approvedId = await CreateReviewAsync(seed.ExpertId, seed.Chains[0].MatchId, rating: 5, sessionType: "online");
            var rejectedId = await CreateReviewAsync(seed.ExpertId, seed.Chains[1].MatchId, rating: 1, sessionType: "yüz_yüze");

            // Yalnız boşluk: IsNullOrWhiteSpace kontrolünün gerçekten "whitespace" ölçtüğünü
            // kanıtlar. Sadece null gönderen bir test, kod null kontrolüne düşürülse de yeşil kalırdı.
            var emptyReason = await fixture.Admin.PostAsJsonAsync(
                $"/admin/reviews/{rejectedId}/reject",
                new { reason = "   " });
            Assert.Equal(HttpStatusCode.UnprocessableEntity, emptyReason.StatusCode);
            var error = await emptyReason.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal("REVIEW_REJECTION_REASON_REQUIRED", error.GetProperty("code").GetString());

            (await fixture.Admin.PostAsync($"/admin/reviews/{approvedId}/approve", null))
                .EnsureSuccessStatusCode();

            const string Reason = "E2E: yorum test verisi olduğu için reddedildi.";
            var reject = await fixture.Admin.PostAsJsonAsync(
                $"/admin/reviews/{rejectedId}/reject",
                new { reason = Reason });
            Assert.Equal(HttpStatusCode.OK, reject.StatusCode);

            var reviews = await FetchMobileReviewsAsync(seed.ExpertId);
            Assert.Contains(reviews, item => item.GetProperty("id").GetGuid() == approvedId);
            Assert.DoesNotContain(reviews, item => item.GetProperty("id").GetGuid() == rejectedId);

            var (rating, count) = await FetchMobileRatingAsync(seed.ExpertId);
            Assert.Equal(1, count);
            Assert.Equal(5d, rating);

            // Red sebebi kaydedildi mi? Admin panelde gösterilen tek gerekçe kaynağı bu alan.
            var rejectedRow = (await FetchAdminReviewsAsync("rejected"))
                .Single(item => item.GetProperty("id").GetGuid() == rejectedId);
            Assert.Equal(Reason, rejectedRow.GetProperty("rejectionReason").GetString());
        });

    /// <summary>Yetki negatifi: danışan token'ı moderasyon yapamaz ve YAN ETKİ de bırakmaz.</summary>
    /// <remarks>
    /// fixture.Client geçerli, imzası doğrulanmış bir Supabase token'ı taşıyor ama
    /// <c>admin_users</c> satırı YOK; bu yüzden beklenen 401 değil 403 (Program.cs'teki
    /// "Admin" politikası + AdminRequirementHandler).
    ///
    /// Kritik ikinci yarı: çağrıdan SONRA yorumun hâlâ mobilde görünmemesi ve admin
    /// kuyruğunda hâlâ 'pending' olması. Yalnız status kodunu doğrulayan bir test,
    /// yetkilendirme reddedilirken işin bir kısmının çoktan yapılmış olduğu bir
    /// sıralama hatasını (önce güncelle, sonra yetkilendir) yakalayamazdı.
    /// </remarks>
    [Fact]
    public Task Danışan_tokenıyla_yorum_onaylanamaz_ve_yorum_mobilde_görünmez_kalır()
        => RunSeededAsync(matchCount: 1, expertStatus: "approved", async seed =>
        {
            var reviewId = await CreateReviewAsync(seed.ExpertId, seed.Chains[0].MatchId, rating: 5, sessionType: "online");

            var response = await fixture.Client.PostAsync($"/admin/reviews/{reviewId}/approve", null);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

            Assert.Empty(await FetchMobileReviewsAsync(seed.ExpertId));

            var stillPending = (await FetchAdminReviewsAsync("pending"))
                .Single(item => item.GetProperty("id").GetGuid() == reviewId);
            Assert.Equal("pending", stillPending.GetProperty("status").GetString());

            var (rating, count) = await FetchMobileRatingAsync(seed.ExpertId);
            Assert.Equal(0d, rating);
            Assert.Equal(0, count);
        });

    /// <summary>
    /// Mobil uçtaki yanıt gövdesi, uygulamanın Zod şemasıyla alan alan aynı olmalı.
    /// </summary>
    /// <remarks>
    /// Kaynak: native-atomic/src/domains/expert/schemas/review.schema.ts (ReviewSchema) ve
    /// native-atomic/src/domains/expert/api/useExpertReviewsQuery.ts
    /// (<c>ReviewSchema.array().parse(raw)</c> — <c>lib/api.ts</c>'teki <c>get()</c> sarmal
    /// açmaz, ham gövde parse edilir; ekran: src/app/expert/[id].tsx:217-265).
    ///
    /// <c>parse</c> (safeParse değil) kullanıldığı için şema tutmazsa query FIRLATIR ve
    /// ekran, HTTP 200 almasına rağmen yorum bölümünü hiç çizmez. Hiçbir status-kodu
    /// testi bunu yakalamaz.
    /// </remarks>
    [Fact]
    public Task Mobil_yorum_yanıtı_uygulamanın_Zod_şemasına_uyar()
        => RunSeededAsync(matchCount: 2, expertStatus: "approved", async seed =>
        {
            var withSessionType = await CreateReviewAsync(
                seed.ExpertId, seed.Chains[0].MatchId, rating: 5, sessionType: "yüz_yüze_online");

            // sessionType HİÇ GÖNDERİLMEDEN: mobil tarafta yorum oluşturma mutation'ı yok,
            // ama CreateReviewDtoValidator alanı açıkça opsiyonel bırakıyor
            // (Must(sessionType is null or ...)) ve DB check constraint'i de null'a izin veriyor.
            // Yani bu gövde üretilebilir bir gerçeklik, teorik bir uç durum değil.
            var withoutSessionType = await CreateReviewAsync(
                seed.ExpertId, seed.Chains[1].MatchId, rating: 3, sessionType: null);

            foreach (var id in (Guid[])[withSessionType, withoutSessionType])
            {
                (await fixture.Admin.PostAsync($"/admin/reviews/{id}/approve", null)).EnsureSuccessStatusCode();
            }

            var raw = await FetchMobileReviewsRawAsync(seed.ExpertId);

            // Sarmal YOK: gövde doğrudan dizi. { data, meta } sarmalına geçilirse
            // ReviewSchema.array().parse patlar — kategorilerin aksine bu uç sarmalsız.
            Assert.Equal(JsonValueKind.Array, raw.ValueKind);

            var full = raw.EnumerateArray().Single(item => item.GetProperty("id").GetGuid() == withSessionType);

            Assert.Equal(withSessionType, full.GetProperty("id").GetGuid());
            Assert.Equal(seed.ExpertId, full.GetProperty("expertId").GetGuid());

            Assert.Equal(JsonValueKind.Number, full.GetProperty("rating").ValueKind);
            Assert.InRange(full.GetProperty("rating").GetInt32(), 1, 5);

            Assert.Equal(JsonValueKind.String, full.GetProperty("comment").ValueKind);
            Assert.Equal(ReviewComment, full.GetProperty("comment").GetString());

            Assert.Equal("yüz_yüze_online", full.GetProperty("sessionType").GetString());

            // z.string().datetime() varsayılan olarak yalnız "Z" sonekini kabul eder;
            // System.Text.Json'ın DateTimeOffset varsayılanı "+00:00" ile biter ve Zod'da
            // "invalid_string: datetime" üretir. UtcDateTimeOffsetJsonConverter bu yüzden var.
            var createdAt = full.GetProperty("createdAt").GetString();
            Assert.NotNull(createdAt);
            Assert.EndsWith("Z", createdAt, StringComparison.Ordinal);
            Assert.True(
                DateTimeOffset.TryParse(createdAt, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out _),
                $"createdAt ISO-8601 olarak ayrıştırılamadı: {createdAt}");

            Assert.Equal(JsonValueKind.String, full.GetProperty("createdAtRelative").ValueKind);
            Assert.False(string.IsNullOrWhiteSpace(full.GetProperty("createdAtRelative").GetString()));

            // ---- sessionType: yok / null / string üçlüsü ----
            // Bu test bir sapmayı ortaya çıkardı ve sapma DÜZELTİLDİ: ReviewSchema'da
            // sessionType yalnız .optional() idi, Zod'da o UNDEFINED'ı kabul eder NULL'ı
            // etmez. Backend ise "sessionType": null yazıyor (Program.cs'te
            // DefaultIgnoreCondition ayarlı değil ve ayarlanamaz — CategorySchema'daki
            // assessmentCategory: z.string().nullable() alanın JSON'da BULUNMASINI şart
            // koşuyor, null'ları atlamak kategori zincirini kırardı).
            // Yön mobil tarafta düzeltildi: artık .nullable().optional().
            // Sapma sürerken belirti şuydu: seans tipi girilmemiş TEK bir yorum,
            // ReviewSchema.array().parse()'ı patlatıp uzmanın TÜM yorum listesini yok eder.
            var bare = raw.EnumerateArray().Single(item => item.GetProperty("id").GetGuid() == withoutSessionType);
            var hasSessionType = bare.TryGetProperty("sessionType", out var sessionType);
            Assert.True(
                !hasSessionType
                    || sessionType.ValueKind is JsonValueKind.String or JsonValueKind.Null,
                $"sessionType mobil şemanın kabul etmediği bir biçimde geldi: "
                + $"{(hasSessionType ? sessionType.ValueKind.ToString() : "alan yok")}. "
                + "Beklenen: alan yok, null, ya da string "
                + "(native-atomic/src/domains/expert/schemas/review.schema.ts).");
        });

    /// <summary>
    /// Aynı ekranın iki çağrısı, onaysız uzmanda AYRI davranıyor: profil kapalı, yorumlar açık.
    /// </summary>
    /// <remarks>
    /// <c>ExpertService.GetByIdAsync</c> uzman 'approved' değilse (ve bakan kişi uzmanın
    /// kendisi değilse) EXPERT_NOT_FOUND/404 fırlatıyor;
    /// <c>ReviewService.ListApprovedForExpertAsync</c>'te uzman durumu HİÇ kontrol edilmiyor.
    /// Mobil ekran (src/app/expert/[id].tsx) iki sorguyu birlikte açtığı için profili
    /// alamayıp yorumları alabiliyor.
    ///
    /// Bu bir "yanlış" değil, bir ASİMETRİ: 0005'teki reviews_public_read_approved RLS
    /// politikası da uzman durumuna bakmıyor, yani DB sözleşmesi C# davranışıyla hemfikir.
    /// Bu yüzden test doğruyu dayatmıyor, MEVCUDU çiviliyor — iki uçtan biri tek başına
    /// değiştirilirse (ör. yorum ucuna status kapısı eklenirse ya da profil kapısı
    /// kaldırılırsa) kırmızı olur ve karar bilinçli alınmış olur.
    /// </remarks>
    [Fact]
    public Task Onaylanmamış_uzmanın_profili_kapalı_ama_onaylı_yorumları_açık_kalır()
        => RunSeededAsync(matchCount: 1, expertStatus: "pending", async seed =>
        {
            var reviewId = await CreateReviewAsync(seed.ExpertId, seed.Chains[0].MatchId, rating: 5, sessionType: "online");
            (await fixture.Admin.PostAsync($"/admin/reviews/{reviewId}/approve", null)).EnsureSuccessStatusCode();

            var profile = await fixture.Client.GetAsync($"/experts/{seed.ExpertId}");
            Assert.Equal(HttpStatusCode.NotFound, profile.StatusCode);
            var error = await profile.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal("EXPERT_NOT_FOUND", error.GetProperty("code").GetString());

            var reviews = await FetchMobileReviewsAsync(seed.ExpertId);
            Assert.Contains(reviews, item => item.GetProperty("id").GetGuid() == reviewId);
        });

    // ------------------------------------------------------------------
    // Zincir kurulumu
    // ------------------------------------------------------------------

    /// <summary>Kurulum + senaryo + temizliği tek yerde bağlar.</summary>
    /// <remarks>
    /// try/finally burada, testlerin içinde değil: senaryo gövdesi kadar SEED'in kendisi
    /// de patlayabilir (ör. listings trigger'ı) ve o durumda da üretilen satırlar
    /// silinmeli. Temizlenmeyen satır bir sonraki koşumu bozardı — özellikle uzman
    /// başına biriken yorumlar, "rating 0'dan 4'e çıktı" iddiasını sessizce yalanlardı.
    /// </remarks>
    private async Task RunSeededAsync(int matchCount, string expertStatus, Func<ReviewChainSeed, Task> scenario)
    {
        var seed = await BeginSeedAsync(matchCount);
        try
        {
            await InsertSeedRowsAsync(seed, expertStatus);
            await scenario(seed);
        }
        finally
        {
            await CleanupAsync(seed);
        }
    }

    /// <summary>Kimlikleri üretir: danışan profili + YENİ bir GoTrue uzman kullanıcısı.</summary>
    /// <remarks>
    /// Her test KENDİ uzmanını üretir (rastgele e-posta). Paylaşılan sabit bir uzman
    /// kullanılsaydı bir testin bıraktığı onaylı yorum diğerinin başlangıç puanını
    /// sıfırdan farklı yapardı ve "0'dan 4'e çıktı" ölçümleri anlamını yitirirdi.
    ///
    /// Bu metot, durum üreten TEK adımı (GoTrue kullanıcısı) en sona bırakır: kendisi
    /// patlarsa geride temizlenecek satır kalmaz, başarılıysa kimlik zaten
    /// <see cref="ReviewChainSeed"/> içinde temizliğe teslim edilmiş olur.
    /// </remarks>
    private async Task<ReviewChainSeed> BeginSeedAsync(int matchCount)
    {
        var me = await fixture.Client.GetFromJsonAsync<JsonElement>("/auth/me");
        var clientId = me.GetProperty("id").GetGuid();

        // ReviewService.CreateAsync ilk kapı olarak role == 'client' istiyor. e2e-client'ın
        // rolü profiles trigger'ının varsayılanından geliyor; bir gün 'expert'e kaysa
        // testler REVIEW_CLIENT_ROLE_REQUIRED/422 ile patlar ve bu moderasyon hatası sanılırdı.
        Assert.Equal("client", me.GetProperty("role").GetString());

        var chains = Enumerable.Range(0, matchCount)
            .Select(_ => new MatchChain(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()))
            .ToArray();

        var expertId = await CreateExpertAuthUserAsync($"e2e-review-{Guid.NewGuid():N}@psikoal.test");

        return new ReviewChainSeed(clientId, expertId, chains, DateTimeOffset.UtcNow);
    }

    /// <summary>GoTrue'da gerçek bir auth kullanıcısı üretir (profiles satırı trigger'la gelir).</summary>
    /// <remarks>
    /// Uzman hiç token almıyor — yorumu danışan yazıyor, uzman tarafı tamamen okuma.
    /// Yine de auth.users satırı ŞART: profiles.id -> auth.users(id) FK'si var ve
    /// experts.id -> profiles(id). Doğrudan auth.users'a insert etmek GoTrue sürümleri
    /// arasında değişen kolon setine bağımlı olurdu; admin API'si sözleşmeli yol.
    /// </remarks>
    private static async Task<Guid> CreateExpertAuthUserAsync(string email)
    {
        using var client = new HttpClient { BaseAddress = new Uri(LocalStack.SupabaseUrl.TrimEnd('/') + "/") };
        client.DefaultRequestHeaders.Add("apikey", LocalStack.ServiceRoleKey);
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", LocalStack.ServiceRoleKey);

        var response = await client.PostAsJsonAsync(
            "auth/v1/admin/users",
            new { email, password = ExpertPassword, email_confirm = true });

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"E2E uzman kullanıcısı üretilemedi ({(int)response.StatusCode}): "
                + await response.Content.ReadAsStringAsync());
        }

        var created = await response.Content.ReadFromJsonAsync<JsonElement>();
        return created.GetProperty("id").GetGuid();
    }

    /// <summary>Uzman + ilan + teklif + eşleşme zincirini doğrudan SQL ile kurar.</summary>
    /// <remarks>
    /// API yolundan gidilseydi ilan admin onayı (PENDING_APPROVAL -> OPEN), teklif
    /// gönderimi ve accept_offer() akışının tamamı koşmak zorunda kalırdı; testin ölçtüğü
    /// halka MODERASYON halkası, teklif akışı değil. SQL seed bu durum makinesini
    /// BİLEREK atlıyor — teklif akışının kendi E2E dosyası ayrıca yazılmalı.
    ///
    /// İlan doğrudan 'MATCHED' açılıyor: enforce_listing_max_active trigger'ı danışanın
    /// PENDING_APPROVAL + OPEN ilanlarını sayıyor (LISTING_MAX_ACTIVE = 3). MATCHED
    /// sayaca girmediği için art arda koşumlar birbirini kilitlemez.
    /// </remarks>
    private static async Task InsertSeedRowsAsync(ReviewChainSeed seed, string expertStatus)
    {
        await using var dataSource = new NpgsqlDataSourceBuilder(LocalStack.PostgresConnectionString).Build();
        await using var connection = await dataSource.OpenConnectionAsync(CancellationToken.None);

        await using (var expert = connection.CreateCommand())
        {
            // İsim boş bırakılamaz: AdminReviewService.ListAsync yorumu profiles ile JOIN'liyor
            // ve ExpertName'i first_name + last_name'den kuruyor. Rolü 'expert'e çekmek de
            // şart değil ama gerçekçi: handle_new_user trigger'ı varsayılan olarak 'client' yazar.
            expert.CommandText =
                """
                update public.profiles
                   set role = 'expert', first_name = 'E2E', last_name = 'Uzman'
                 where id = @expert_id;

                insert into public.experts (id, title, specializations, experience_years, bio, status)
                values (@expert_id, 'Klinik Psikolog', '{}', 5, 'E2E moderasyon zinciri uzmanı.', @expert_status)
                on conflict (id) do update set status = excluded.status;
                """;
            expert.Parameters.AddWithValue("expert_id", seed.ExpertId);
            expert.Parameters.AddWithValue("expert_status", expertStatus);
            await expert.ExecuteNonQueryAsync(CancellationToken.None);
        }

        foreach (var chain in seed.Chains)
        {
            await using var command = connection.CreateCommand();

            // offers.match_id -> matches(id) ile matches.accepted_offer_id -> offers(id)
            // karşılıklı FK: teklif önce match'siz yazılır, match kurulduktan sonra
            // bağlanır. accept_offer() fonksiyonu da aynı sırayı izliyor.
            //
            // match.status = 'COMPLETED': ReviewService.CreateAsync ACTIVE bir eşleşmede
            // MATCH_NOT_ACTIVE/422 fırlatır — yorum ancak tamamlanmış/bırakılmış eşleşmeye yazılır.
            command.CommandText =
                """
                insert into public.listings
                  (id, client_id, title, description, specialization, budget_min, budget_max, preferred_session_type, status)
                values
                  (@listing_id, @client_id, 'E2E yorum zinciri ilanı', '', '{}', 100, 200, 'online', 'MATCHED');

                insert into public.offers
                  (id, listing_id, expert_id, price, description, session_type, status)
                values
                  (@offer_id, @listing_id, @expert_id, 150, '', 'online', 'ACCEPTED');

                insert into public.matches
                  (id, listing_id, accepted_offer_id, client_id, expert_id, status)
                values
                  (@match_id, @listing_id, @offer_id, @client_id, @expert_id, 'COMPLETED');

                update public.offers set match_id = @match_id where id = @offer_id;
                """;
            command.Parameters.AddWithValue("listing_id", chain.ListingId);
            command.Parameters.AddWithValue("offer_id", chain.OfferId);
            command.Parameters.AddWithValue("match_id", chain.MatchId);
            command.Parameters.AddWithValue("client_id", seed.ClientId);
            command.Parameters.AddWithValue("expert_id", seed.ExpertId);
            await command.ExecuteNonQueryAsync(CancellationToken.None);
        }
    }

    /// <summary>Testin ürettiği HER satırı geri alır.</summary>
    /// <remarks>
    /// İki kök yetiyor, gerisini FK cascade sürüklüyor:
    ///   listings -> offers (listing_id), matches (listing_id) -> reviews (match_id)
    ///   auth.users -> profiles (id) -> experts (id)
    /// Bildirimler ayrı: notifications.user_id danışanı işaret ediyor ve danışan
    /// silinmiyor, o yüzden tek tek temizleniyor. Zaman filtresi bilinçli — aynı
    /// danışanın başka bir testten kalan bildirimi varsa ona dokunmayalım.
    ///
    /// audit_logs TEMİZLENMİYOR: 0001'deki prevent_audit_mutation trigger'ı tabloyu
    /// append-only yapıyor (delete de update de exception). Moderasyon kaydı orada
    /// birikir; tasarım gereği ve testin bozabileceği bir şey değil.
    /// </remarks>
    private static async Task CleanupAsync(ReviewChainSeed seed)
    {
        await using var dataSource = new NpgsqlDataSourceBuilder(LocalStack.PostgresConnectionString).Build();
        await using var connection = await dataSource.OpenConnectionAsync(CancellationToken.None);
        await using var command = connection.CreateCommand();

        command.CommandText =
            """
            delete from public.notifications
             where user_id = @client_id
               and type in ('REVIEW_APPROVED', 'REVIEW_REJECTED')
               and created_at >= @since;

            delete from public.listings where id = any(@listing_ids);

            delete from auth.users where id = @expert_id;
            """;
        command.Parameters.AddWithValue("client_id", seed.ClientId);
        command.Parameters.AddWithValue("since", seed.StartedAt);
        command.Parameters.AddWithValue("listing_ids", seed.Chains.Select(chain => chain.ListingId).ToArray());
        command.Parameters.AddWithValue("expert_id", seed.ExpertId);
        await command.ExecuteNonQueryAsync(CancellationToken.None);
    }

    // ------------------------------------------------------------------
    // Uç sarmalayıcıları
    // ------------------------------------------------------------------

    private async Task<Guid> CreateReviewAsync(Guid expertId, Guid matchId, int rating, string? sessionType)
    {
        // sessionType null ise alan gövdeye hiç konmuyor: mobil uygulama da göndermeyeceği
        // için (yorum oluşturma mutation'ı henüz yok) en gerçekçi gövde bu.
        object payload = sessionType is null
            ? new { matchId, rating, comment = ReviewComment }
            : new { matchId, rating, comment = ReviewComment, sessionType };

        var response = await fixture.Client.PostAsJsonAsync($"/experts/{expertId}/reviews", payload);
        var body = await response.Content.ReadAsStringAsync();

        // Gövdeyi mesaja koymak şart: seed kırılırsa belirti MATCH_NOT_ACTIVE /
        // REVIEW_CLIENT_ROLE_REQUIRED gibi bir alan hatası olur, çıplak "beklenen 200"
        // mesajı ise hatayı moderasyonda aratırdı.
        Assert.True(
            response.StatusCode == HttpStatusCode.OK,
            $"Yorum oluşturulamadı ({(int)response.StatusCode}): {body}");

        using var document = JsonDocument.Parse(body);
        var created = document.RootElement;

        // Yeni yorum HER ZAMAN pending doğar; onay adımı olmadan mobilde görünmemesinin
        // sebebi bu varsayılan. ReviewDto'da status alanı olmadığı için buradan
        // doğrulanamaz — kanıtı Onay_bekleyen_yorum_... testindeki admin kuyruğu.
        return created.GetProperty("id").GetGuid();
    }

    /// <summary>Mobil uygulamanın yorum listesi çağrısı.</summary>
    /// <remarks>
    /// fixture.Mobile DEĞİL: GET /experts/{id}/reviews [Authorize] taşıyor, yani uç
    /// kamuya açık değil ve token'sız istemci 401 alır. Mobil ekran her zaman oturumlu
    /// açıldığı için doğru taklit fixture.Client.
    /// </remarks>
    private async Task<JsonElement> FetchMobileReviewsRawAsync(Guid expertId)
    {
        var response = await fixture.Client.GetAsync($"/experts/{expertId}/reviews");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }

    private async Task<JsonElement[]> FetchMobileReviewsAsync(Guid expertId)
        => [.. (await FetchMobileReviewsRawAsync(expertId)).EnumerateArray()];

    /// <summary>Uzman profilindeki yıldız ve yorum sayısı — expert_ratings view'undan gelir.</summary>
    private async Task<(double Rating, int ReviewCount)> FetchMobileRatingAsync(Guid expertId)
    {
        var response = await fixture.Client.GetAsync($"/experts/{expertId}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var expert = await response.Content.ReadFromJsonAsync<JsonElement>();
        return (expert.GetProperty("rating").GetDouble(), expert.GetProperty("reviewCount").GetInt32());
    }

    private async Task<JsonElement[]> FetchAdminReviewsAsync(string? status)
    {
        var url = status is null ? "/admin/reviews" : $"/admin/reviews?status={status}";
        var raw = await fixture.Admin.GetFromJsonAsync<JsonElement>(url);
        return [.. raw.EnumerateArray()];
    }

    private sealed record MatchChain(Guid ListingId, Guid OfferId, Guid MatchId);

    private sealed record ReviewChainSeed(
        Guid ClientId,
        Guid ExpertId,
        IReadOnlyList<MatchChain> Chains,
        DateTimeOffset StartedAt);
}
