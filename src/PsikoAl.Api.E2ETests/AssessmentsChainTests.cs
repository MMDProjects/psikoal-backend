using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Npgsql;

namespace PsikoAl.Api.E2ETests;

/// <summary>
/// "Admin panelden psikolojik testi düzenlediğimde mobil uygulamada karşılığı değişiyor mu?"
/// sorusunun makine cevabı.
/// </summary>
/// <remarks>
/// Zincir: admin token -> PATCH /admin/assessments -> Postgres -> GET /assessment ve
/// POST /assessment/submit -> mobil uygulamanın Zod şeması
/// (native-atomic/src/domains/assessment/schemas/assessment.schema.ts).
/// Domain bilinçli seçildi: admin ucu ile mobil ucu AYNI tabloyu FARKLI filtreyle okuyor
/// (AdminAssessmentService.ListAsync hiçbir filtre uygulamaz, AssessmentService.ListAsync
/// ise .Where(a => a.IsActive) uygular) — "admin panelde duruyor ama mobilde yok"
/// hatasının doğal yuvası. Üstüne puanlama bandı metinleri doğrudan kullanıcıya
/// psikolojik yönlendirme olarak gösteriliyor; o metnin admin'den mobile bozulmadan
/// gitmesi bu domainin en değerli halkası.
/// </remarks>
/// <remarks>
/// Test verisi API ile ÜRETİLEMEZ: AdminAssessmentsController'da POST ve DELETE yok,
/// yalnız GET/GET{id}/PATCH/PATCH score-rules var. Bu yüzden kurulum Npgsql ile
/// doğrudan SQL. Seed'deki üç test (Kaygı/Depresyon/Stres) BİLEREK hiç kurcalanmıyor:
/// GET /assessment/active onların sort_order'ına (10/20/30) bağlı ve PATCH'lenirlerse
/// başka senaryolar sessizce bozulurdu. Kendi ağacımız sort_order 900 ile ekleniyor ki
/// /assessment/active'i seed testlerinden ÇALMASIN.
/// </remarks>
/// <remarks>
/// <c>Category=E2E</c> trait'i şart: birim job'u <c>--filter "Category!=E2E"</c> ile
/// dışlıyor, e2e.yml ise <c>--filter "Category=E2E"</c> ile yalnız bunları koşuyor.
/// </remarks>
[Trait("Category", "E2E")]
[Collection(E2ECollection.Name)]
public sealed class AssessmentsChainTests(E2EFixture fixture) : IAsyncLifetime
{
    private const string SeedDescription = "E2E testinin ürettiği geçici psikolojik test.";
    private const int SeedEstimatedMinutes = 3;
    private const int SeedQuestionCount = 3;

    // sort_order 900: seed testleri 10/20/30 kullanıyor. Küçük bir değer verilseydi
    // GET /assessment/active bizim testimizi döndürür ve mobil uygulamanın varsayılan
    // test akışını (native-atomic/src/app/assessment/index.tsx) bu test sınıfı çalarken
    // başka senaryolar nedensiz kırmızı olurdu.
    private const int SeedSortOrder = 900;

    // Değerler bant sınırlarını KESİN ayırsın diye 0/2/5 seçildi: üç sorudan toplanan
    // hiçbir kombinasyon bant sınırına (4/5, 9/10) belirsiz düşmüyor.
    private const string SeedOptionsJson =
        """[{"id":"e1","text":"Hiç","value":0},{"id":"e2","text":"Bazen","value":2},{"id":"e3","text":"Sıklıkla","value":5}]""";

    private const string LowSummary = "E2E düşük bant özeti.";
    private const string ModerateSummary = "E2E orta bant özeti.";
    private const string HighSummary = "E2E yüksek bant özeti.";

    // Kategori guid'li ve benzersiz: GET /assessment?category= ile testimizin TEK BAŞINA
    // çekilebilmesi, aşağıdaki Single() çağrılarının kanıt değeri taşımasını sağlıyor.
    // Slug regex'i ^[a-z0-9-]+$ — guid'in "N" formatı zaten yalnız [0-9a-f].
    private readonly string _category = $"e2e-{Guid.NewGuid():N}"[..12];

    private string _title = string.Empty;
    private Guid _assessmentId;
    private IReadOnlyList<Guid> _questionIds = [];

    public async Task InitializeAsync()
    {
        _title = $"E2E Test {_category}";

        await using var dataSource = new NpgsqlDataSourceBuilder(LocalStack.PostgresConnectionString).Build();
        await using var connection = await dataSource.OpenConnectionAsync(CancellationToken.None);

        _assessmentId = await InsertAssessmentAsync(connection);
        _questionIds = await InsertQuestionsAsync(connection);
        await InsertScoreRulesAsync(connection);
    }

    /// <summary>Ürettiğimiz ağacı kaldırır.</summary>
    /// <remarks>
    /// Tek DELETE yeterli: assessment_questions, assessment_score_rules ve
    /// assessment_results üzerindeki FK'lar <c>on delete cascade</c>
    /// (bkz. supabase/migrations/20260806214557_assessments.sql). Testlerin ürettiği
    /// assessment_results satırları da böylece gidiyor. listings.assessment_result_id
    /// ise <c>on delete set null</c>, yani cascade bir ilanı düşürmez.
    /// </remarks>
    public async Task DisposeAsync()
    {
        if (_assessmentId == Guid.Empty)
        {
            return;
        }

        await using var dataSource = new NpgsqlDataSourceBuilder(LocalStack.PostgresConnectionString).Build();
        await using var connection = await dataSource.OpenConnectionAsync(CancellationToken.None);
        await using var command = connection.CreateCommand();
        command.CommandText = "delete from public.assessments where id = @id;";
        command.Parameters.AddWithValue("id", _assessmentId);
        await command.ExecuteNonQueryAsync(CancellationToken.None);
    }

    /// <summary>
    /// Admin panelde düzenlenen başlık/süre mobil test listesine aynen düşmeli.
    /// </summary>
    /// <remarks>
    /// PATCH'ten ÖNCEKİ okuma bu testin belkemiği: SQL seed'i zaten doğru değeri yazdığı
    /// için, yalnız PATCH SONRASI bakan bir test, PATCH hiçbir şey yapmasa da yeşil
    /// kalırdı — yani hiçbir şey kanıtlamazdı.
    /// </remarks>
    [Fact]
    public async Task Admin_düzenlediği_test_başlığı_ve_süresi_mobil_test_listesinde_yansır()
    {
        var before = SingleMobileItem(await FetchMobileEnvelopeAsync(_category));
        Assert.Equal(_title, before.GetProperty("title").GetString());
        Assert.Equal(SeedEstimatedMinutes, before.GetProperty("estimatedMinutes").GetInt32());

        var newTitle = $"E2E Güncel {_category}";
        const int newMinutes = 11;

        var patch = await PatchAssessmentAsync(fixture.Admin, newTitle, newMinutes, isActive: true);
        Assert.Equal(HttpStatusCode.OK, patch.StatusCode);

        var envelope = await FetchMobileEnvelopeAsync(_category);

        // Sarmal SÖZLEŞMESİ: GET /assessment yalnız { data: [...] } döner, meta YOKTUR.
        // Bu asimetri gerçek: GET /assessment/results/my aynı domainde { data, meta }
        // döndürüyor. useAssessmentListQuery.ts "raw.data ?? []" ile toleranslı,
        // useMyAssessmentResultsQuery.ts ise meta'yı ZORUNLU kılıyor. Buraya bir gün
        // meta eklenirse sözleşme sessizce değişmiş olur; aşağıdaki satır onu yakalar.
        Assert.Equal(JsonValueKind.Array, envelope.GetProperty("data").ValueKind);
        Assert.False(
            envelope.TryGetProperty("meta", out _),
            "GET /assessment sarmalında meta OLMAMALI; sözleşme değiştiyse mobil tarafla birlikte güncellenmeli.");

        var after = SingleMobileItem(envelope);
        Assert.Equal(newTitle, after.GetProperty("title").GetString());
        Assert.Equal(newMinutes, after.GetProperty("estimatedMinutes").GetInt32());
        Assert.Equal(_assessmentId, after.GetProperty("id").GetGuid());
        Assert.Equal(_category, after.GetProperty("category").GetString());

        // questionCount, AssessmentService içindeki AYRI bir GroupBy sayımından geliyor —
        // başlıkla aynı sorgudan değil. SQL ile eklediğimiz soru sayısına eşitlemek
        // o sayımı da kanıta bağlar.
        Assert.Equal(SeedQuestionCount, after.GetProperty("questionCount").GetInt32());

        // Alan adı/tipi kayarsa mobil taraf safeParse ile BOŞ LİSTE gösterir ve HTTP 200
        // döndüğü için hiçbir status-kodu testi bunu yakalamaz. Kaynak sözleşme:
        // native-atomic/src/domains/assessment/schemas/assessment.schema.ts
        // -> AssessmentListItemSchema { id, title, category, estimatedMinutes, questionCount }
        foreach (var field in (string[])["id", "title", "category"])
        {
            Assert.Equal(JsonValueKind.String, after.GetProperty(field).ValueKind);
        }

        foreach (var field in (string[])["estimatedMinutes", "questionCount"])
        {
            Assert.Equal(JsonValueKind.Number, after.GetProperty(field).ValueKind);
        }
    }

    /// <summary>
    /// Pasifleştirme mobil listeden düşürür ama admin listesinde kaydı KORUR.
    /// </summary>
    /// <remarks>
    /// Çiftli iddia şart. Yalnız "mobilde yok" denseydi test, kaydın silinmiş olmasını da
    /// kabul ederdi. İkinci yarı ise filtre asimetrisinin bekçisi: AdminAssessmentService
    /// .ListAsync'e bir gün yanlışlıkla .Where(a => a.IsActive) eklenirse admin paneli
    /// pasif testleri kaybeder ve onları geri açmanın yolu kalmaz (POST ucu da yok).
    /// </remarks>
    [Fact]
    public async Task Admin_pasifleştirdiği_test_mobil_listesinden_düşer_ama_admin_listesinde_kalır()
    {
        var beforeMobile = await FetchMobileItemsAsync(category: null);
        Assert.Contains(beforeMobile, item => item.GetProperty("id").GetGuid() == _assessmentId);

        var patch = await PatchAssessmentAsync(fixture.Admin, _title, SeedEstimatedMinutes, isActive: false);
        patch.EnsureSuccessStatusCode();

        var afterMobile = await FetchMobileItemsAsync(category: null);
        Assert.DoesNotContain(afterMobile, item => item.GetProperty("id").GetGuid() == _assessmentId);

        // Uç tamamen boş dönseydi (ör. sorgu bozulsaydı) yukarıdaki DoesNotContain de
        // yeşil olurdu. Seed'in üç testi her koşumda burada olmak zorunda.
        Assert.NotEmpty(afterMobile);

        // GET /admin/assessments sarmalsız DİZİ döner (IReadOnlyList doğrudan).
        var adminList = await fixture.Admin.GetFromJsonAsync<JsonElement>("/admin/assessments");
        var adminItem = adminList.EnumerateArray()
            .Single(item => item.GetProperty("id").GetGuid() == _assessmentId);

        Assert.False(adminItem.GetProperty("isActive").GetBoolean());
        Assert.Equal(_title, adminItem.GetProperty("title").GetString());
        Assert.Equal(SeedSortOrder, adminItem.GetProperty("sortOrder").GetInt32());
    }

    /// <summary>
    /// Pasifleştirilmiş teste gönderim yapılamaz — listeden gizlemek yetmez.
    /// </summary>
    /// <remarks>
    /// Mobil tarafta test id'si önbellekte kalmış olabilir (useAssessmentListQuery
    /// staleTime 10 dk), yani "listede görünmüyor" bir kısıt DEĞİL. Asıl kısıt
    /// AssessmentService.SubmitAsync içindeki "assessment is null || !assessment.IsActive"
    /// koruması; bu test onun gerçekten koştuğunu ölçüyor.
    /// Beklenen kod 404: DomainExceptionMiddleware *_NOT_FOUND kodlarını 404'e, iş kuralı
    /// ihlallerini 422'ye çeviriyor. Pasifleştirme ÖNCESİ aynı gövdenin 200 döndüğü de doğrulanıyor —
    /// yoksa test, gönderimin bambaşka bir sebeple (bozuk payload) patlamasıyla da
    /// yeşil olurdu.
    /// </remarks>
    [Fact]
    public async Task Pasifleştirilmiş_teste_mobil_gönderim_404_ASSESSMENT_NOT_FOUND_ile_reddedilir()
    {
        var payload = NewSubmitPayload(0, 2, 0);

        var accepted = await fixture.Mobile.PostAsJsonAsync("/assessment/submit", payload);
        Assert.Equal(HttpStatusCode.OK, accepted.StatusCode);

        var patch = await PatchAssessmentAsync(fixture.Admin, _title, SeedEstimatedMinutes, isActive: false);
        patch.EnsureSuccessStatusCode();

        var rejected = await fixture.Mobile.PostAsJsonAsync("/assessment/submit", payload);
        Assert.Equal(HttpStatusCode.NotFound, rejected.StatusCode);

        var error = await rejected.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("ASSESSMENT_NOT_FOUND", error.GetProperty("code").GetString());
    }

    /// <summary>
    /// Admin'in yazdığı puan bandı metni, mobil sonuç ekranına BİREBİR ulaşmalı.
    /// </summary>
    /// <remarks>
    /// Domainin en değerli halkası: bu metinler kullanıcıya psikolojik yönlendirme olarak
    /// gösteriliyor. Zincir tek testte kuruluyor — admin detay ucundan bandın id'si
    /// bulunur (id SQL'den DEĞİL, admin okuma ucundan gelir; böylece o uç da kanıta
    /// dahil olur), metin PATCH'lenir, token'sız gönderim yapılır ve dönen sonuçta aynı
    /// metin beklenir. Aynı anda üç şey kanıtlanır: skorun Values toplamından
    /// hesaplandığı, FindMatchingRuleAsync'in doğru bandı seçtiği ve AssessmentResult'a
    /// bandın metninin KOPYALANDIĞI.
    /// </remarks>
    [Fact]
    public async Task Admin_güncellediği_puan_bandı_metni_mobil_test_sonucunda_aynen_döner()
    {
        var detail = await fixture.Admin.GetFromJsonAsync<JsonElement>($"/admin/assessments/{_assessmentId}");
        var moderate = detail.GetProperty("scoreRules")
            .EnumerateArray()
            .Single(rule => rule.GetProperty("level").GetString() == "moderate");

        var ruleId = moderate.GetProperty("id").GetGuid();
        Assert.Equal(5, moderate.GetProperty("minScore").GetInt32());
        Assert.Equal(9, moderate.GetProperty("maxScore").GetInt32());

        // GÖRÜNMEZ TIE-BREAKER: assessment_score_rules.sort_order tabloda ve entity'de VAR,
        // FindMatchingRuleAsync çakışan bantlarda kazananı .OrderBy(SortOrder).First() ile
        // seçiyor — ama AdminScoreRuleDto bu alanı TAŞIMIYOR ve UpdateAdminScoreRuleDto ile
        // yazılamıyor. Yani hangi bandın uygulanacağını admin'in ne görebildiği ne de
        // değiştirebildiği bir alan belirliyor. Alan DTO'ya eklendiği gün bu satır kırmızı
        // olur ve kararı bilinçli almış oluruz.
        Assert.False(
            moderate.TryGetProperty("sortOrder", out _),
            "AdminScoreRuleDto sortOrder taşımıyor; taşımaya başladıysa mobil/admin sözleşmesi gözden geçirilmeli.");

        var summary = $"E2E orta bant guncel ozet {_category}";
        string[] suggestions = [$"E2E oneri A {_category}", $"E2E oneri B {_category}"];

        var patch = await fixture.Admin.PatchAsJsonAsync(
            $"/admin/assessments/score-rules/{ruleId}",
            new { summary, suggestions });
        patch.EnsureSuccessStatusCode();

        var patched = await patch.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(summary, patched.GetProperty("summary").GetString());

        // Eşikler API'den DEĞİŞTİRİLEMEZ: UpdateAdminScoreRuleDto yalnız (Summary,
        // Suggestions) taşıyor. PATCH'ten sonra min/max/level'ın aynı kaldığını doğrulamak,
        // "sadece metin yazılabilir" kısıtını sözleşmeye bağlar.
        Assert.Equal(5, patched.GetProperty("minScore").GetInt32());
        Assert.Equal(9, patched.GetProperty("maxScore").GetInt32());
        Assert.Equal("moderate", patched.GetProperty("level").GetString());

        // 5 + 2 + 0 = 7 -> [5, 9] bandı. Token'sız istemci bilinçli: POST /assessment/submit
        // auth OPSİYONEL ve mobil uygulama testi girişsiz de yaptırıyor.
        var submit = await fixture.Mobile.PostAsJsonAsync("/assessment/submit", NewSubmitPayload(5, 2, 0));
        Assert.Equal(HttpStatusCode.OK, submit.StatusCode);

        var result = await submit.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(7, result.GetProperty("score").GetInt32());
        Assert.Equal("moderate", result.GetProperty("level").GetString());
        Assert.Equal(summary, result.GetProperty("summary").GetString());

        var returnedSuggestions = result.GetProperty("suggestions")
            .EnumerateArray()
            .Select(item => item.GetString()!)
            .ToArray();
        Assert.Equal(suggestions, returnedSuggestions);

        // createdAt "Z" ile BİTMEK zorunda. Mobil şema (assessment.schema.ts:
        // AssessmentResultSchema.createdAt = z.string().datetime()) offset'li formatı
        // (+03:00) REDDEDER. Bugün geçmesinin tek sebebi Program.cs'te kayıtlı
        // UtcDateTimeOffsetJsonConverter. O converter kaldırılırsa mobil sonuç ekranı
        // HTTP 200 aldığı hâlde parse hatasıyla boş kalırdı — bu satır o günü yakalar.
        var createdAt = result.GetProperty("createdAt").GetString();
        Assert.EndsWith("Z", createdAt, StringComparison.Ordinal);
        Assert.True(
            DateTimeOffset.TryParse(createdAt, CultureInfo.InvariantCulture, DateTimeStyles.None, out _),
            $"createdAt ISO-8601 olarak ayrıştırılamadı: {createdAt}");
    }

    /// <summary>
    /// Danışan token'ı ne testi ne puan bandını değiştirebilir.
    /// </summary>
    /// <remarks>
    /// Diğer testlerin yeşil olmasının sebebinin GERÇEK admin kimliği olduğunu, yetkinin
    /// kapalı olması olmadığını gösterir. İki katman ayrı ayrı ölçülüyor: token'sız istek
    /// yalnız AUTHENTICATION'ı, danışan token'ı ise AUTHORIZATION'ı ölçer (geçerli,
    /// imzası doğrulanmış Supabase JWT'si var ama admin_users satırı YOK).
    /// Sonda kaydın hiç değişmediği de doğrulanıyor: reddeden ama önce yazan bir uç
    /// yalnız status koduna bakan bir testten kaçardı. Mobil uygulamanın hesabıyla puan
    /// bandı metinlerinin değiştirilebilmesi sessiz ve ağır bir açık olurdu — o metinler
    /// doğrudan kullanıcılara psikolojik yönlendirme olarak gösteriliyor.
    /// </remarks>
    [Fact]
    public async Task Danışan_token_ile_test_veya_puan_bandı_güncellenemez()
    {
        var detail = await fixture.Admin.GetFromJsonAsync<JsonElement>($"/admin/assessments/{_assessmentId}");
        var lowRule = detail.GetProperty("scoreRules")
            .EnumerateArray()
            .Single(rule => rule.GetProperty("level").GetString() == "low");
        var ruleId = lowRule.GetProperty("id").GetGuid();

        var anonymousAssessment = await PatchAssessmentAsync(fixture.Mobile, "E2E yetkisiz", 99, isActive: false);
        Assert.Equal(HttpStatusCode.Unauthorized, anonymousAssessment.StatusCode);

        var clientAssessment = await PatchAssessmentAsync(fixture.Client, "E2E danisan", 99, isActive: false);
        Assert.Equal(HttpStatusCode.Forbidden, clientAssessment.StatusCode);

        var hijackPayload = new { summary = "E2E yetkisiz metin", suggestions = (string[])["E2E yetkisiz oneri"] };

        var anonymousRule = await fixture.Mobile.PatchAsJsonAsync(
            $"/admin/assessments/score-rules/{ruleId}", hijackPayload);
        Assert.Equal(HttpStatusCode.Unauthorized, anonymousRule.StatusCode);

        var clientRule = await fixture.Client.PatchAsJsonAsync(
            $"/admin/assessments/score-rules/{ruleId}", hijackPayload);
        Assert.Equal(HttpStatusCode.Forbidden, clientRule.StatusCode);

        var afterDetail = await fixture.Admin.GetFromJsonAsync<JsonElement>($"/admin/assessments/{_assessmentId}");
        Assert.Equal(_title, afterDetail.GetProperty("title").GetString());
        Assert.Equal(SeedEstimatedMinutes, afterDetail.GetProperty("estimatedMinutes").GetInt32());
        Assert.True(afterDetail.GetProperty("isActive").GetBoolean());
        Assert.Equal(
            LowSummary,
            afterDetail.GetProperty("scoreRules")
                .EnumerateArray()
                .Single(rule => rule.GetProperty("id").GetGuid() == ruleId)
                .GetProperty("summary")
                .GetString());
    }

    /// <summary>
    /// Giriş yapmış kullanıcının sonucu yalnız kendisine açık; anonim sonuç id ile açık kalır.
    /// </summary>
    /// <remarks>
    /// Sözleşme §5. Önceden id'yi bilen herkes başkasının sonucunu okuyabiliyordu —
    /// özel nitelikli sağlık verisi. Başkasına 403 değil 404 dönülür: kaydın varlığı
    /// bile sızdırılmaz.
    /// </remarks>
    [Fact]
    public async Task Sahibi_olan_sonuç_yalnız_sahibine_açık_anonim_sonuç_herkese_açık()
    {
        var owned = await fixture.Client.PostAsJsonAsync("/assessment/submit", NewSubmitPayload(0, 2, 0));
        Assert.Equal(HttpStatusCode.OK, owned.StatusCode);
        var ownedId = (await owned.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        Assert.Equal(HttpStatusCode.OK, (await fixture.Client.GetAsync($"/assessment/results/{ownedId}")).StatusCode);

        var anonymousRead = await fixture.Mobile.GetAsync($"/assessment/results/{ownedId}");
        Assert.Equal(HttpStatusCode.NotFound, anonymousRead.StatusCode);
        Assert.Equal(
            "ASSESSMENT_RESULT_NOT_FOUND",
            (await anonymousRead.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString());

        Assert.Equal(HttpStatusCode.NotFound, (await fixture.Admin.GetAsync($"/assessment/results/{ownedId}")).StatusCode);

        var anonymous = await fixture.Mobile.PostAsJsonAsync("/assessment/submit", NewSubmitPayload(0, 2, 0));
        Assert.Equal(HttpStatusCode.OK, anonymous.StatusCode);
        var anonymousId = (await anonymous.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        Assert.Equal(HttpStatusCode.OK, (await fixture.Mobile.GetAsync($"/assessment/results/{anonymousId}")).StatusCode);
    }

    /// <summary>
    /// Gönderim testin gerçek sorularına karşı doğrulanır; puan istemciden alınmaz.
    /// </summary>
    /// <remarks>
    /// Eskiden QuestionId'ler hiç okunmuyordu ve skor yalnız her cevabın İLK değerini
    /// topluyordu: uydurma soru kimlikleri ve seçenekte olmayan değerlerle istenen
    /// seviye üretilebiliyordu. Artık her soru tam bir kez cevaplanmalı, her değer o
    /// sorunun seçeneklerinden biri olmalı; aksi 422 ASSESSMENT_ANSWERS_INVALID.
    /// </remarks>
    [Fact]
    public async Task Testle_eşleşmeyen_cevaplar_422_ASSESSMENT_ANSWERS_INVALID_ile_reddedilir()
    {
        var fabricatedQuestions = new
        {
            assessmentId = _assessmentId,
            answers = (object[])
            [
                new { questionId = "bu-soru-hicbir-teste-ait-degil", values = (int[])[5] },
                new { questionId = Guid.NewGuid().ToString(), values = (int[])[2] },
                new { questionId = "42", values = (int[])[0] },
            ],
        };

        var fabricated = await fixture.Mobile.PostAsJsonAsync("/assessment/submit", fabricatedQuestions);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, fabricated.StatusCode);
        Assert.Equal(
            "ASSESSMENT_ANSWERS_INVALID",
            (await fabricated.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString());

        // 99 hiçbir seçeneğin değeri değil: istemci puanı şişiremez.
        var inflated = await fixture.Mobile.PostAsJsonAsync("/assessment/submit", NewSubmitPayload(99, 0, 0));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, inflated.StatusCode);
    }


    private async Task<Guid> InsertAssessmentAsync(NpgsqlConnection connection)
    {
        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            insert into public.assessments
                (title, description, category, estimated_minutes, sort_order, is_active)
            values (@title, @description, @category, @minutes, @sort_order, true)
            returning id;
            """;
        command.Parameters.AddWithValue("title", _title);
        command.Parameters.AddWithValue("description", SeedDescription);
        command.Parameters.AddWithValue("category", _category);
        command.Parameters.AddWithValue("minutes", SeedEstimatedMinutes);
        command.Parameters.AddWithValue("sort_order", SeedSortOrder);

        return (Guid)(await command.ExecuteScalarAsync(CancellationToken.None))!;
    }

    private async Task<IReadOnlyList<Guid>> InsertQuestionsAsync(NpgsqlConnection connection)
    {
        var ids = new List<Guid>(SeedQuestionCount);

        for (var order = 1; order <= SeedQuestionCount; order++)
        {
            await using var command = connection.CreateCommand();

            // options sütunu jsonb; parametre metin olarak gidiyor, cast şart.
            command.CommandText =
                """
                insert into public.assessment_questions
                    (assessment_id, text, type, sort_order, options)
                values (@assessment_id, @text, 'single_choice', @sort_order, cast(@options as jsonb))
                returning id;
                """;
            command.Parameters.AddWithValue("assessment_id", _assessmentId);
            command.Parameters.AddWithValue("text", $"E2E soru {order}");
            command.Parameters.AddWithValue("sort_order", order);
            command.Parameters.AddWithValue("options", SeedOptionsJson);

            ids.Add((Guid)(await command.ExecuteScalarAsync(CancellationToken.None))!);
        }

        return ids;
    }

    private async Task InsertScoreRulesAsync(NpgsqlConnection connection)
    {
        await using var command = connection.CreateCommand();

        // level değerleri migration'daki check constraint'e uymak zorunda:
        // check (level in ('low', 'moderate', 'high')).
        command.CommandText =
            """
            insert into public.assessment_score_rules
                (assessment_id, min_score, max_score, level, summary, suggestions, sort_order)
            values
                (@assessment_id, 0, 4, 'low', @low_summary, @low_suggestions, 1),
                (@assessment_id, 5, 9, 'moderate', @moderate_summary, @moderate_suggestions, 2),
                (@assessment_id, 10, 21, 'high', @high_summary, @high_suggestions, 3);
            """;
        command.Parameters.AddWithValue("assessment_id", _assessmentId);
        command.Parameters.AddWithValue("low_summary", LowSummary);
        command.Parameters.AddWithValue("low_suggestions", (string[])["E2E dusuk oneri"]);
        command.Parameters.AddWithValue("moderate_summary", ModerateSummary);
        command.Parameters.AddWithValue("moderate_suggestions", (string[])["E2E orta oneri"]);
        command.Parameters.AddWithValue("high_summary", HighSummary);
        command.Parameters.AddWithValue("high_suggestions", (string[])["E2E yuksek oneri"]);

        await command.ExecuteNonQueryAsync(CancellationToken.None);
    }

    /// <summary>
    /// PATCH ucu kısmi DEĞİL: UpdateAdminAssessmentDto tüm alanları taşır ve
    /// AdminAssessmentService.UpdateAsync hepsini yazar. Category ve SortOrder bu yüzden
    /// her çağrıda seed değerine SABİTLENİYOR — kaymaları hâlinde kategori filtreli
    /// sorgular boş dönerdi ve /assessment/active seed testlerinden çalınırdı.
    /// </summary>
    private Task<HttpResponseMessage> PatchAssessmentAsync(
        HttpClient client,
        string title,
        int estimatedMinutes,
        bool isActive)
        => client.PatchAsJsonAsync(
            $"/admin/assessments/{_assessmentId}",
            new
            {
                title,
                description = SeedDescription,
                category = _category,
                estimatedMinutes,
                isActive,
                sortOrder = SeedSortOrder,
            });

    /// <summary>Gerçek soru kimlikleriyle gönderim gövdesi üretir (her soruya tek değer).</summary>
    private object NewSubmitPayload(params int[] values) => new
    {
        assessmentId = _assessmentId,
        answers = _questionIds
            .Select((questionId, index) => new
            {
                questionId = questionId.ToString(),
                values = (int[])[values[index]],
            })
            .ToArray(),
        email = (string?)null,
    };

    private async Task<JsonElement> FetchMobileEnvelopeAsync(string? category)
    {
        var url = category is null
            ? "/assessment"
            : $"/assessment?category={Uri.EscapeDataString(category)}";

        return await fixture.Mobile.GetFromJsonAsync<JsonElement>(url);
    }

    private async Task<JsonElement[]> FetchMobileItemsAsync(string? category)
        => [.. (await FetchMobileEnvelopeAsync(category)).GetProperty("data").EnumerateArray()];

    /// <summary>
    /// Kategori benzersiz olduğu için Single() bir iddiadır: birden fazla satır dönerse
    /// izolasyon bozulmuş demektir ve test bunu sessizce geçmemeli.
    /// </summary>
    private static JsonElement SingleMobileItem(JsonElement envelope)
        => envelope.GetProperty("data").EnumerateArray().Single();
}
