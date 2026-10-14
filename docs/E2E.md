# Uçtan Uca Test Altyapısı

Bu doküman "**admin panelden eklediğimde mobil uygulamada geliyor mu?**" sorusunun
otomatik cevabını nasıl aldığımızı anlatır.

## Zincir

```
Playwright  →  Blazor admin   →  PsikoAl.Api  →  Postgres  →  PsikoAl.Api  →  Mobil uygulama  ←  Maestro
(tarayıcı)     (localhost:5057)  (localhost:5089)  (54322)     public uçlar     (Android emülatör)
```

**Dört** katman. Her katman bir öncekinin doğruladığını yeniden doğrulamaz;
**kırmızı olan en alttaki katman hatanın yerini söyler.** Katman 3 (mobil sözleşme)
emülatör gerektirmediği için PR'larda da koşuyor.

| # | Katman | Araç | Nerede | Test | Süre |
|---|---|---|---|---|---|
| 1 | API zinciri | xUnit + gerçek Postgres | `src/PsikoAl.Api.E2ETests/` | 49 | ~30 sn |
| 2 | Admin paneli | Playwright + Chromium | `e2e/admin/` | 7 | ~20 sn |
| 3 | Mobil sözleşme | Jest + uygulamanın gerçek Zod şemaları | `psikoal-app/src/lib/contract.live.test.ts` | 17 | ~20 sn |
| 4 | Mobil uygulama | Maestro + Android emülatör | `e2e/mobile/` | 3 akış | ~4 dk |

## Hızlı başlangıç (lokal)

```bash
cd psikoal-backend

# 1. Stack: Supabase + API + admin paneli + test kimlikleri
source scripts/e2e-stack.sh
```

> `source ... | tail` **yazmayın** — boru hattı alt kabukta koşar, `export`'lar
> kaybolur ve sonraki adımlar "ortam değişkeni tanımsız" der.

```bash
# 2. Demo verisi — gerçek uçlar üzerinden (59 çağrı, tekrar koşulabilir)
node scripts/seed-demo.mjs

# 3. Katman 1 — API zinciri
dotnet test src/PsikoAl.Api.E2ETests --filter "Category=E2E"

# 4. Katman 2 — admin paneli (mobil adımın kullanacağı slug'ı da üretir)
cd e2e/admin && npx playwright test && cd ../..

# 5. Katman 3 — mobil sözleşme (native-atomic içinde; seed'e İHTİYAÇ DUYAR)
cd ../native-atomic
PSIKOAL_LIVE_API=http://localhost:5089 npx jest src/lib/contract.live.test.ts
cd ../psikoal-backend

# 6. Katman 4 — mobil
scripts/e2e-mobile.sh

# Bitince
scripts/e2e-stack-down.sh
```

İlk kurulum bir kez: `cd e2e/admin && npm install && npx playwright install chromium`
Test verisi birikirse: `scripts/e2e-clean.sh`

## Katman 1 — API zinciri (49 test)

`src/PsikoAl.Api.E2ETests/`

Mevcut `PsikoAl.Api.ContractTests`'ten farkı **gerçek veritabanı**: `ApiFactory`
`ConnectionStrings:Postgres` enjekte etmiyor ve `Program.cs` boş connection
string'de Npgsql'i hiç konfigüre etmiyor — yani contract testleri veritabanısız
koşuyor ve yalnız 401 doğrulayabiliyor (51 assertion'ın 47'si `Unauthorized`).
`E2EFactory` gerçek lokal Postgres'e bağlanır; "yazdım → okudum" ancak böyle
doğrulanabilir.

| Dosya | Zincir | Test |
|---|---|---|
| `AdminToMobileChainTests.cs` | Kategori ekle/pasifleştir → mobil kategori listesi | 5 |
| `ExpertsChainTests.cs` | Uzman onayla/reddet/revizyon → mobil uzman profili | 6 |
| `ListingsChainTests.cs` | İlan onayla/reddet → mobil feed | 5 |
| `ReviewsChainTests.cs` | Yorum onayla → uzman profili + rating | 6 |
| `NotificationsChainTests.cs` | Bildirim gönder → mobil bildirim + okunmamış sayacı | 5 |
| `AssessmentsChainTests.cs` | Test/soru/puan kuralı → mobil test listesi + skorlama | 6 |
| `SystemUsersChainTests.cs` | Sistem ayarı / kullanıcı dondurma → mobil davranış | 7 |
| `OffersChainTests.cs` | Uzman teklif gönderir → danışan ilanında görür | 6 |
| `DashboardChainTests.cs` | Admin panelin açılış sayfası gerçek Postgres üzerinde yüklenir | 3 |

### Şema testleri neden ayrı

Mobil uygulama her yanıtı Zod ile doğruluyor. Alan adı ya da tip değişirse
`safeParse` düşer ve kullanıcı **boş liste** görür. HTTP hâlâ 200 döndüğü için
hiçbir status-kodu testi bunu yakalamaz. Her domainde bir "mobil şemaya uyar"
testi tam bu boşluğu kapatıyor — ve bu testler kurulum sırasında **üç gerçek
sözleşme kırılması** buldu (aşağıya bakın).

### `null` vs `undefined` — sözleşmenin tek sahibi

Zod 4'te `.optional()` yalnız `undefined`'ı kabul eder; `null` parse hatasıdır.
Backend ise nullable alanları `null` olarak yazıyor.

**`Program.cs`'te `DefaultIgnoreCondition = WhenWritingNull` ayarlanamaz**:
`CategorySchema`'daki `assessmentCategory: z.string().nullable()` (optional
DEĞİL) alanın JSON'da **bulunmasını** şart koşuyor — null'ları atlamak kategori
zincirini kırardı. Bu yüzden yön mobil tarafta düzeltildi: null gelebilen her
alan `.nullable().optional()`.

Bu kararın tek sahibi `ListingsChainTests.AssertNullableOptional`. Orada
gevşetirseniz `SystemUsersChainTests` kırmızıya döner; kararı orada değiştirin.

### Neden `Category=E2E` trait'i

Proje `PsikoAl.slnx` içinde — `-warnaserror` kapısından geçiyor. Ama `ci.yml`'in
birim test adımı `--filter "Category!=E2E"` ile onu dışlar (orada lokal stack
yok). E2E'ler `e2e.yml`'de `--filter "Category=E2E"` ile koşar.

## Katman 2 — Admin paneli (Playwright)

`e2e/admin/`

Üç Blazor'a özgü tuzak ve çözümleri:

**1. Prerender yarışı.** Blazor Server önce statik HTML gönderir; DOM hazırdır
ama `@bind` ve `OnClick` bağlı değildir. O anda yapılan `fill()` DOM değerini
değiştirir, C# alanına hiç ulaşmaz, form boş gider. Çözüm: `gotoInteractive()`
SignalR WebSocket'i açılana kadar bekler.

**2. Oturum circuit'e bağlı.** `AdminSessionState` scoped bir servistir, token'ı
yalnız circuit belleğinde tutar. `page.goto('/categories')` yeni circuit açar,
oturum ölür, sayfa `/login`'e döner. **Bu gerçekten yaşandı** — kurulumda iki
test "tablo bulunamadı" diye kırmızı oldu. Çözüm: giriş sonrası geçişler
`navigateTo()` ile nav bağlantısına tıklayarak (SPA geçişi, circuit korunur).
Aynı sebeple `storageState` ile oturum önbelleğe **alınamaz**.

**3. Rate limit.** API'de `auth` politikası IP başına dakikada 10 istek. Her
test dosyası bir kez login olmak zorunda olduğundan `workers: 1` ve
`fullyParallel: false`.

**Panel sayfalarının tamamı artık kapsamda.** Katman bir dönem yalnız Login ve
Kategoriler'i doğruluyordu; diğer sekiz sayfa ne testle ne elle açılmamıştı. O
boşlukta **Dashboard tamamen kırıktı**: `/admin/dashboard/stats` 500 dönüyordu
çünkü `GroupBy(x => x.CreatedAt.UtcDateTime.Date)` Npgsql tarafından SQL'e
çevrilemiyor ("No coercion operator is defined between types 'DateTimeOffset' and
'Nullable<DateTime>'"). Belirti sessizdi: sayfa hatayı yakalayıp boş kart gösteriyor,
konsolda hiçbir şey çıkmıyor. `admin-pages.spec.ts` tüm sayfaları tek oturumda gezer
ve dashboard sayaçlarının gerçekten SAYI taşıdığını iddia eder.

Sayfa başlığı için `getByRole('heading')` kullanılır ve `.first()` ŞARTTIR: bazı
sayfalarda aynı metinli ikinci bir başlık var (Testler) ve strict mode iki eşleşmede
hata veriyor — mesaj "başlık görünmüyor" gibi okunuyor, oysa başlık iki tanedir.
`.page-title` sınıfı denendi ve DOM'a çıkmıyor.

Artifact politikası: `trace: retain-on-failure`, `screenshot: only-on-failure`,
`video: off`, `retries: CI ? 1 : 0`. Lokalde retry yok — flaky'i gizlemek onu
kaybetmektir.

## Katman 3 — Mobil sözleşme (Jest, gerçek şemalar)

`psikoal-app/src/lib/contract.live.test.ts`

Uygulamanın **gerçek** Zod şemalarını, çalışan API'nin **gerçek** yanıtlarına karşı
koşturur — danışan ve uzman gözüyle ayrı ayrı, 17 kontrol.

**Neden ayrı bir katman:** bu sınıf hatanın belirtisi yok. Backend nullable bir alanı
`null` yazar, şemada `.nullable()` eksikse `safeParse` düşer, hook throw eder ve
kullanıcı **boş liste** görür — HTTP hâlâ 200'dür. Ne Katman 1 (durum kodlarına
bakıyor) ne Katman 2 (tarayıcı) bunu görebilir. İki kez tam olarak böyle kırıldı:
`OfferSchema.matchId` (ilana gelen tüm teklifler kayboldu) ve `MatchSchema.email`
(gizlilik düzeltmesi e-postayı null yapınca eşleşme ekranı boşalacaktı).

**Varsayılan olarak ATLANIR.** Birim testleri ağa bağlı olmamalı; süit yalnız
`PSIKOAL_LIVE_API` verildiğinde koşar (`describe.skip` deseni).

**Seed olmadan hiçbir şey ölçmez:** `z.array(...)` boş diziyi kabul eder, yani veri
yoksa süit yeşil görünür ama hiçbir alanı doğrulamamış olur. `seed-demo.mjs` bu
yüzden ön koşul, tercih değil.

**Uçlar arası zarf asimetrisi bilinçli:** liste uçları `{ data, meta }` sarmalıyor,
ama `GET /experts/{id}/reviews` `IReadOnlyList<ReviewDto>` yani ÇIPLAK dizi
döndürüyor. Test bunu ayrıca ele alıyor.

## Katman 4 — Mobil uygulama (Maestro)

`e2e/mobile/` — akışlar burada, uygulama kodu `psikoal-app` reposunda.

| Akış | Soru |
|---|---|
| `01-app-launches.yaml` | Uygulama açılıyor mu? |
| `02-client-can-log-in.yaml` | Giriş yapılıyor, veri API'den geliyor mu? |
| `03-admin-added-category-appears.yaml` | Admin'in eklediği kategori listede mi? |

Bugünkü durum: **01 yeşil**, 02 ve 03 kırmızı — ama bu bir test hatası değil,
aşağıdaki açık bulgunun ta kendisi.

`03` tek başına anlamlı değildir: kategoriyi kendisi eklemez.
`e2e/admin/tests/category-chain.spec.ts` ekler ve slug'ı devreder. Bir Playwright
testi emülatör süremez, bir Maestro akışı tarayıcı süremez — zinciri
`scripts/e2e-mobile.sh` (lokalde) veya CI adımı birleştirir.

### Ortam gereksinimleri

| Ne | Neden | Not |
|---|---|---|
| JDK 17+ | Maestro ve Android Gradle Plugin | `JAVA_HOME` |
| Android SDK + `cmdline-tools` | `avdmanager`, `sdkmanager` | `ANDROID_HOME` |
| **`ANDROID_AVD_HOME`** | `emulator` binary'si `ANDROID_USER_HOME`'u OKUMAZ | aşağıya bakın |
| Bir AVD | `emulator -avd psikoal-e2e` | |
| Maestro CLI | akışları koşturur | `~/.maestro-install/maestro/bin` |

**`ANDROID_AVD_HOME` tuzağı:** `avdmanager` (yeni) `ANDROID_USER_HOME`'u okur,
`emulator` (36.6.11) okumaz — o yalnız `ANDROID_AVD_HOME`, `ANDROID_SDK_HOME/avd`
ve `$HOME/.android/avd` sırasına bakar. AVD'ler standart dışı bir konumdaysa
`avdmanager list avd` onları görür ama `emulator -list-avds` **boş döner** ve
belirti "Unknown AVD name" olur.

**Ağ köprüsü — `adb reverse`, `10.0.2.2` DEĞİL:** Emülatörden host'a
`127.0.0.1` ile ulaşılmaz. Klasik çözüm `10.0.2.2` (host loopback'inin emülatör
içindeki takma adı) ama **Windows'ta güvenlik duvarı gelen bağlantıyı kesiyor**;
belirti uygulamanın içinde "No network connection" oluyor ve hiçbir logda
görünmüyor — akış "ekran gelmedi" diye düşer, sebep tamamen saklanır. Bu
yaşandı ve teşhis ancak ekrandaki hata mesajı okunarak konuldu.

Bunun yerine `adb reverse tcp:5089 tcp:5089` kullanılıyor: cihazın kendi
`localhost:5089`'u host'un 5089'una tünellenir. Güvenlik duvarından etkilenmez
ve Linux CI'da da aynı çalışır — tek yol, iki platform. APK bu yüzden
`EXPO_PUBLIC_API_URL=http://localhost:5089` ile derlenir.

Erişim kontrolü **host'tan** yapılır: Android imajında `curl` YOK, `adb shell
curl` her zaman başarısız olur ve sahte uyarı üretir.

**Maestro değişkenleri `-e` ile geçirilir, export YETMEZ.** Maestro export
edilmiş kabuk değişkenlerini akışlardaki `${VAR}` yerine koymuyor; yerine
literal `"undefined"` yazıyor. Belirti "giriş başarısız" olur ve kimlik hatası
sanılır — gerçekte forma `undefined` yazılmıştır. Bu da ekranda görülerek
teşhis edildi.

**Paket adı:** `app.json`'da `android.package` **tanımsız**, bu yüzden Expo
prebuild `com.anonymous.psikoal` üretiyor. `scripts/e2e-mobile.sh` bunu
`android/app/build.gradle`'dan tespit ediyor, sabit yazmıyor. Mağazaya
göndermeden önce gerçek bir paket adı belirlenmeli (yayınlandıktan sonra
değiştirilemez).

### testID sözleşmesi

Akışlar `testID` ile bağlanır, metinle değil: ürün metni değişir, aynı metin
birden çok yerde geçer ve rol bazlı ekranlar aynı konumda farklı metin gösterir
(`(tabs)/_layout.tsx`: uzman "Fırsatlar", danışan "Keşfet"). Adlandırma kuralı:
`psikoal-app/docs/E2E-TESTID.md`.

## Test kimlikleri

Tek kaynak: `src/PsikoAl.Api.E2ETests/SupabaseTestIdentity.cs`.
Kullanıcılar GoTrue'da gerçekten üretilir, token'lar gerçekten imzalanır.

| Hesap | E-posta | Şifre | `admin_users` satırı |
|---|---|---|---|
| Admin | `e2e-admin@psikoal.test` | `E2eAdminPass123!` | var (`super_admin`) |
| Danışan | `e2e-client@psikoal.test` | `E2eClientPass123!` | **yok** (bilinçli) |
| Uzman | `e2e-expert@psikoal.test` | `E2eExpertPass123!` | yok |
| Teklif uzmanı | `e2e-offer-expert@psikoal.test` | `E2eOfferExpertPass123!` | yok |

Danışan hesabının admin satırı olmaması bir özellik:
`Danışan_token_ile_admin_ucuna_erişilemez` testi yetki sınırını **geçerli bir
token'la** ölçüyor — token'sız istekten daha güçlü bir kanıt.

`e2e-offer-expert` ayrı bir hesap olmak zorunda: `ExpertsChainTests` her testte
kendi uzman **satırını** siliyor. Aynı hesap paylaşılsaydı iki dosya birlikte
koştuğunda belirti rastgele `EXPERT_NOT_FOUND` olur ve sözleşme hatası sanılırdı.

**Demo hesapları bunlardan AYRIDIR.** `scripts/seed-demo.mjs`
`demo-client@psikoal.test` ve `demo-expert-*@psikoal.test` hesaplarını kullanır.
Demo verisi bir dönem `e2e-client` üzerine basıldı ve `SystemUsersChainTests`
kırmızıya döndü (o test `LISTING_MAX_ACTIVE`'i o hesapla ölçüyor); ayrıca testin
kendi temizliği demo verisini sildi. Test fixture'ı ile demo verisi aynı hesabı
paylaşamaz.

Bu sabitler **dört yerde** görünür ve birebir aynı olmak zorundadır:
`SupabaseTestIdentity.cs`, `scripts/e2e-stack.sh`, `scripts/e2e-mobile.sh`,
`.github/workflows/e2e.yml`. Ayrışırlarsa belirti "401" olur ve sözleşme hatası
sanılır.

## CI

`.github/workflows/e2e.yml` — `ci.yml`'den ayrı, çünkü bu job'lar Docker'lı
Supabase, iki .NET süreci ve bir Android emülatörü kaldırıyor.

| Olay | Koşan job'lar |
|---|---|
| PR (her dal) | `api-chain`, `admin-ui`, `mobile-contract` |
| push `dev`/`main` | + `mobile-ui` (emülatör — pahalı) |
| `workflow_dispatch` | hepsi |

`mobile-contract` PR'larda koşuyor çünkü emülatör gerektirmiyor: Supabase + API +
seed + Jest. Ölçtüğü hata sınıfının belirtisi olmadığı için erken koşması önemli.

**Paket adı CI'da SABİT YAZILMAZ.** `mobile-ui` bir dönem `MAESTRO_APP_ID: com.psikoal`
sabitini taşıyordu; gerçek paket adı `com.anonymous.psikoal` (app.json'da
`android.package` tanımsız olduğu için Expo prebuild bunu üretiyor). Maestro kurulu
olmayan bir paketi hedefliyor, akışlar "ekran görünmüyor" diye düşüyor ve sebep
hiçbir logda görünmüyordu. Artık "APK derle" adımı `build.gradle`'dan okuyup
`GITHUB_ENV`'e yazıyor — lokal `scripts/e2e-mobile.sh` ile aynı desen.

`mobile-ui` iki repoyu birden checkout eder. `psikoal-app` **public** olduğu için
varsayılan `GITHUB_TOKEN` yeter; ek PAT gerekmez. Repo private yapılırsa
`Contents: Read` yetkili fine-grained bir PAT secret'ı eklenmelidir.

Slug tek yerde üretilir (`admin-ui` job output'u) ve `mobile-ui`'a aktarılır —
dosya üzerinden devretmek job sınırını geçemezdi (ayrı runner, ayrı disk).

## Bu altyapının bulduğu gerçek hatalar

Kurulum bir doğrulama egzersizi değildi; şunları ortaya çıkardı ve hepsi
düzeltildi:

| Bulgu | Belirti | Düzeltme |
|---|---|---|
| `RequireHttpsMetadata` her zaman açık | API lokal Supabase'e karşı **hiç** ayağa kalkamıyor, `/health` dahil her istek 500 | `Program.cs`: yalnız Development'ta gevşetilir |
| `react-native-mmkv` ölü bağımlılık | Native Android build **hiç** alınamıyor (`nitro-modules` eksik) | `package.json`'dan kaldırıldı; kod hiç import etmiyordu |
| `@expo/ui` ölü bağımlılık | Uygulama açılışta çöküyor (`ClassNotFoundException`) | kaldırıldı (`expo-glass-effect` ile birlikte) |
| `review.schema.ts` `sessionType` | Seans tipi girilmemiş **tek** yorum, uzmanın **tüm** yorum listesini yok ediyor | `.nullable().optional()` |
| `listing.schema.ts` `city`/`viewerOfferId`/`assessmentResult` | Feed'in tamamı parse hatasına düşüyor, kullanıcı boş liste görüyor | `.nullable().optional()` |
| Uzman profilinde URL doğrulaması yok | `avatar.png` HTTP 200 ile kaydediliyor, mobilde uzman detayı boş ekran | `UrlRuleExtensions` + eksik `UpdateExpertProfileDtoValidator` |
| `PATCH /experts/profile` doğrulamasız | POST'ta reddedilen değer PATCH ile serbestçe yazılabiliyor | validator dosyası oluşturuldu |
| `supabase db psql` alt komutu yok | `ci.yml`'deki `pg_cron` kapısı her koşumda sessizce warning verip geçiyor | `db query --local` |

## KAPANAN BULGU — Keşfet ekranı release build'de boştu

**Kök neden bulundu ve düzeltildi (22 Ağustos 2026).** Reanimated/worklet şüphesi
**çürüdü**: giriş ekranındaki `DecorCircles` (Animated kullanıyor) release'de
sorunsuz çiziliyordu.

Gerçek sebep `AppRefreshControl`'dü. React Native, Android'de `refreshControl`
elementini **klonlayıp scroll içeriğinin tamamını ona `children` olarak veriyor**
(`ScrollView.js`: `cloneElement(refreshControl, {style}, <NativeScrollView>…)`).
`AppRefreshControl` `children` ve `style`'ı iletmiyordu; içerik yere düşüyordu.
iOS'ta `refreshControl` yalnız bir çocuk olduğu için belirti **yalnız Android'de**
görülüyordu. Pull-to-refresh kullanan **20 ekranı** etkiliyordu.

Teşhisi kolaylaştıran adım: görünüm ağacını `uiautomator dump` ile okumak. Düğümler
"görünmez" değil, **hiç yoktu** — bu, sorunu "veri gelmiyor"dan "render edilmiyor"a
daraltan tek kanıttı.

## Bu turda bulunan diğer hatalar (hepsi düzeltildi)

Ekran ekran gezme turu, belirtisi olmayan on bir hata çıkardı. Hepsinin ortak yanı:
HTTP 200, istisna yok, mevcut testler yeşil, kullanıcı eksik ekran görüyor.

| Bulgu | Belirti | Nerede kilitlendi |
|---|---|---|
| `AppRefreshControl` `children` iletmiyordu | Android'de 20 ekran boş | (yukarıda) |
| axios diziyi `status[]=` diye serileştiriyordu | 4 filtre sessizce yok sayılıyordu | `api.test.ts` |
| Yenileme isteği 401 alınca kuyruk kilitleniyordu | Girişten sonra bildirimler sonsuza kadar yükleniyordu | `api.test.ts` |
| `OfferSchema.matchId` null kabul etmiyordu | İlana gelen tüm teklifler "Henüz teklif gelmedi" | `OffersChainTests` + `offer.schema.test.ts` |
| `SYSTEM` şablonu admin başlığını yok sayıyordu | Her duyuru "Duyuru" başlıklı | `NotificationsChainTests` |
| `Share*` bayrakları hiçbir yerde süzmüyordu | Kapatılan iletişim paylaşımı yine paylaşılıyordu | `MatchMapperPrivacyTests` |
| Test gönderimi "sonuçlarım"ı tazelemiyordu | Yeni çözülen test ilana eklenemiyordu | `useSubmitAssessmentMutation.test.tsx` |
| Kayıt akışı `onboardIntent` kurmuyordu | Uzman onboarding'i erişilemezdi | (elle doğrulandı) |
| `SendOfferSchema` boş "opsiyonel" başlığı reddediyordu | Teklif formu kilitleniyordu | `offer.schema.test.ts` |
| `message` alanı hata KODUNU taşıyordu | Kullanıcı `AUTH_INVALID_CREDENTIALS` görüyordu | `ErrorMessagesTrTests` |
| `OFFER_EXPERT_ROLE_REQUIRED` ölü koddu | Danışana "uzman profilinizi tamamlayın" deniyordu | `OffersChainTests` |
| Dashboard sorgusu Npgsql'de çevrilemiyordu | Panelin AÇILIŞ sayfası sessizce boştu | `DashboardChainTests` + `admin-pages.spec.ts` |


## Bilinen sınırlar

- **Ödeme (Iyzico) ve blog/öneri domainleri kapsam dışı.** Ödeme sandbox hesabı
  gerektiriyor; blog/`ui_content` dilim 9'da gelecek.
- **Playwright test verisi temizlenmiyor.** Kategori silme ucu yok;
  `scripts/e2e-clean.sh` elle çalıştırılır. CI'da her koşum temiz stack'te
  başladığı için etkisi yok.
- **`audit_logs` temizlenemiyor.** `trg_audit_logs_no_update` tetikleyicisi
  tabloyu append-only yapıyor; her onay/red satırı kalıcı. Benzersizlik kısıtı
  olmadığı için sonraki koşumu bozmuyor, ama satır sayısı her koşumda artıyor.
- **CI emülatörü API 34, lokalde doğrulanan API 37.** CI'da doğrulanmadı;
  `mobile-ui` ilk koşumunda uyumsuzluk çıkarsa `api-level` yükseltilmeli.
- **Gradle `EXPO_PUBLIC_*` değişikliğini görmez.** `createBundleReleaseJsAndAssets`
  task'ı ortam değişkeni değişince "up-to-date" kalıyor ve APK ESKİ API adresiyle
  çıkıyor — belirti "ağ yok" olur, sebep görünmez. API adresi değiştiğinde
  `android/app/build/generated/assets/createBundleReleaseJsAndAssets` silinmeli.
- **Emülatör CPU'ya duyarlı.** Docker Supabase + iki .NET süreci + SwiftShader
  yazılım render aynı anda koşarken emülatör ANR veriyor ("Process system isn't
  responding"). `-gpu auto` (donanım GPU) ve en az 4 çekirdek gerekiyor.
- **Eş zamanlı iki koşum aynı lokal stack'i bozar.** `ExpertsChainTests` sabit
  bir uzman hesabının satırını her testte siliyor; iki koşum aynı anda bakarsa
  belirti rastgele `EXPERT_NOT_FOUND` olur. Tek koşumda sorun yok.

## Değişiklik yaparken

- Yeni bir E2E testi eklerken **önce kırmızı gördüğünü doğrula.** Kurulumda ana
  zincir testi kasten bozulup kırmızı olduğu doğrulandı; bu adım atlanırsa "hep
  yeşil" bir test hiçbir şey ölçmez.
- **Sapmayı ölçen sonda, zincirin bayrak testinin içine konmaz.** `ExpertsChainTests`'te
  tam bu oldu: bozuk URL sondası ana teste gömülüydü, `AssertMobileExpertSchema`
  fırlatıyordu ve altındaki `status`/`acceptsOffers`/`isVerified` iddiaları hiç
  koşmuyordu — zincirin tek pozitif kanıtı kalıcı olarak kırmızıydı. Sonda ayrı
  bir teste taşındı.
- **Global sistem ayarını değiştiren test, `finally`'de ÖNCE ayarı geri yazar,**
  sonra satırları siler. Ters sırada bir Npgsql hatası ayarı bozuk bırakır ve
  başka dosyalar alakasız sebeplerle kırmızı olur.
- `e2e.yml`'deki `Test sayısı siperi` test paketinin sessizce boşalmasını
  engeller. Test eklerken eşiği yukarı çekmeyi unutma.
- Mobil ekrana yeni `testID` eklerken önce akışı yaz, sonra işaretle.
  Kullanılmayan `testID` bakımsız kalır ve yanlış güven verir.
