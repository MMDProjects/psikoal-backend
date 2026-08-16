# Backend Gereksinimleri

> Bu doküman, frontend'in **hiçbir iş mantığı hesaplamaması** prensibiyle yapılan refactoring sonucunda backend'in karşılaması gereken sözleşmeyi tanımlar. `mock-db/handlers/` altındaki handler'lar bu sözleşmenin çalışan referans implementasyonudur — gerçek backend birebir bu şekilde davranmalıdır.

## Genel Kurallar

- Tüm liste yanıtları `{ data: [...], meta: { page, total, perPage, ...ekstra } }` zarfındadır.
- Hata yanıtları `{ code, message, field? }` şeklindedir (bkz. CLAUDE.md §6).
- Tarihler ISO-8601; göreli etiketler (`"2 gün önce"`) **server'da** hesaplanıp hazır string olarak döner.
- İsim maskeleme (`"Zeynep Yılmaz" → "Zeynep Y."`) ve avatar baş harfleri (`initials`) **server'da** üretilir. Gerçek backend, eşleşme öncesi uzman görünümlerinde danışanın tam adını **hiç göndermemelidir** (mock'ta oturum ayrımı olmadığından ikisi birlikte dönüyor).

## Endpoint Sözleşmesi (mock'ta implement edilen)

### Listing
| Endpoint | Ek alanlar / parametreler |
|---|---|
| `GET /listings` | Param: `specialization[]`, `sessionType[]`, `budgetMin`, `budgetMax`, `sort` (`newest\|budget_desc\|budget_asc\|offer_asc`). Yanıtta her listing: `budgetLabel` ("₺500 – ₺900"), `clientDisplayName` (maskelenmiş), `client.initials`, `createdAtRelative` |
| `GET /listings/my` | Param: `status`. `meta.activeCount` (OPEN sayısı) |
| `GET /listings/:id` | `viewerHasOffered: boolean`, `viewerOfferId?: string` (istek yapan uzmanın bu ilana teklifi) |
| `GET /listings/:id/offers` | Teklifler `serveOffer` zenginleştirmesiyle döner |

### Offer
| Endpoint | Ek alanlar / parametreler |
|---|---|
| `GET /offers/my` | Param: `status`. `meta.pendingCount`. Her offer: `createdAtRelative`, `expert.initials`, `listing.clientDisplayName`, `listing.client.initials` |
| `POST /offers` | `expert` embed'i (name/title/rating) server'da auth kullanıcısının uzman kaydından doldurulur — frontend göndermez |

### Match
| Endpoint | Ek alanlar / parametreler |
|---|---|
| `GET /matches` | Param: `status[]` (`ACTIVE` / `COMPLETED,RELEASED`). `meta.activeCount`, `meta.pastCount`. Her match: `client.initials`, `expert.initials`, `createdAtRelative`, embedded `listing` (budgetLabel dahil) + `offer` |

### Expert
- `rating` ve `reviewCount` **her yanıtta reviews tablosundan hesaplanır** (statik alan tutulmaz).
- `initials`, `acceptsOffers` (= `status === 'approved'`) alanları döner. Frontend teklif uygunluğunu `role === 'client' && expert.acceptsOffers` ile belirler.
- `GET /experts/:id/reviews` → her review'a `createdAtRelative`.

### Payment
- `GET /payment/packages` → her pakete `originalPrice` (indirimsiz toplam). Frontend `unitPrice × sessionCount` hesaplamaz.
- Cüzdan verisi `wallet.json` yapısında döner.

### Blog
- `GET /blogs` → param `category`, `limit` (ana sayfa 3 öğe için `limit=3` kullanır). Her blog: `liked` (oturum kullanıcısı beğenmiş mi), `likeCount`.
- `POST /blogs/:slug/like` → toggle; yanıt `{ likeCount, liked }` — frontend optimistic artırma/azaltma yapmaz.

### Assessment
- `GET /assessment` → her test `questionCount` içerir.
- `GET /assessment/results/my` → `{ data, meta.total }`, `createdAt` desc sıralı (ilk eleman = son sonuç).
- `POST /assessment/submit` → skorlama tamamen server'da (`score`, `level`, `summary`, `suggestions`).

### Notification (yeni)
- `GET /notifications` → `{ data, meta.unreadCount }`. Her bildirim: `id`, `type` (`OFFER_RECEIVED | OFFER_ACCEPTED | LISTING_EXPIRING | SYSTEM`), `title`, `body`, `createdAt`, `timeLabel` (server'da hesaplanan göreli zaman), `read`.

### Category
- `GET /categories/:slug` → `expertCount`, `completedMatchCount` server'da hesaplanır. Uzman `specializations` değerleri kategori adlarıyla birebir aynı sözlükten gelir (alias eşlemesi kaldırıldı — veri normalize edildi).

### Client
- Tüm client yanıtlarında `initials`.

## Frontend'de Kalan Salt Görsel Formatlama
(İstenirse backend'e taşınabilir; iş mantığı içermez.)

- `₺` para formatı: `price.toLocaleString('tr-TR')` (Chip/summary gösterimleri)
- Mutlak tarih yazımı: `core/utils/formatDate.ts` (`short | long | time | dayMonth | dayMonthShort`)
- `rating.toFixed(1)` — sayı gösterim hassasiyeti
- `getInitials`/`getFullName` — yalnızca lokal auth kullanıcısı (server verisi olmayan) için fallback
- Durum → renk/ikon/etiket eşlemeleri: `LISTING_STATUS_CONFIG`, `OFFER_STATUS_CONFIG`, `MATCH_STATUS_CONFIG`, `RESULT_LEVEL_CONFIG`, `NOTIFICATION_TYPE_CONFIG`, `SESSION_TYPE_LABELS` (sunum konfigürasyonu)
- Yıldız dolu/yarım/boş türetimi (`RatingRow`) ve quiz ilerleme çubuğu (`useAssessmentEngine`) — saf UI durumu

## Bilinen Veri Borçları (gerçek backend'de düzeltilecek)
- Mock offer/match embed'lerindeki uzman adları ("Dr. Ayşe Kaya") experts tablosuyla ünvan/id bazında tam tutarlı değil (77777… id'li uzman experts.json'da yok). Gerçek backend join ile üretir.
- `CreateListingForm` ve expert onboarding formları manuel validasyon kullanıyor; `CreateListingSchema`/`ExpertOnboardingSchema` Zod şemalarıyla React Hook Form'a taşınması önerilir (mesaj tekilleştirme).

*Son güncelleme: 18 Temmuz 2026*
