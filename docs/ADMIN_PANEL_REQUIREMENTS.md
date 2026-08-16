# ADMIN PANEL REQUIREMENTS — PsikoAl

> Bu doküman, C# (N-tier) backend + admin paneli geliştirilirken sözleşme olarak kullanılır.
> Prensip: **Frontend hiçbir iş mantığı içermez.** Admin paneli, platformdaki her varlığın her aşamasını görüntüleyebilir, değiştirebilir ve onaylayabilir. Referans veri modeli: `mock-db/data/*.json` (her JSON dosyası = bir Supabase tablosu). İş mantığı referansı: `mock-db/handlers/*.js` ve `docs/BACKEND_REQUIREMENTS.md`.

---

## 1. ONAY / MODERASYON AKIŞLARI

Kullanıcının ürettiği içerik, kullanıcıya "yayınlandı" gibi görünse de admin onayından geçmeden karşı tarafa gösterilmez.

### 1.1 İlan Onayı (listing)

Yeni durum makinesi (onay katmanı eklenmiş hali):

```
PENDING_APPROVAL → OPEN → MATCHED → (CLOSED | EXPIRED)
                 → REJECTED_BY_ADMIN
```

| Kural | Açıklama |
|---|---|
| Oluşturma | Danışan ilan oluşturur → durum `PENDING_APPROVAL`. Danışan kendi ekranında ilanını "Yayında" gibi görür (veya "İncelemede" — UX kararı), ancak uzman feed'ine (Fırsatlar) DÜŞMEZ. |
| Onay | Admin onaylar → `OPEN`. `expiresAt = onay tarihi + LISTING_EXPIRE_DAYS`. Uzmanlara görünür olur. |
| Red | Admin reddeder → `REJECTED_BY_ADMIN` + **red sebebi zorunlu** (danışana bildirim gider, ilan düzenlenip yeniden onaya gönderilebilir). |
| Düzenleme | Danışan `OPEN` ilanı düzenlerse ilan tekrar `PENDING_APPROVAL`'a döner. |
| Limit | `PENDING_APPROVAL` + `OPEN` toplamı danışan başına max `LISTING_MAX_ACTIVE` sayılır. |

Admin panel ekranı: bekleyen ilanlar kuyruğu (en eski üstte), ilan detayı (başlık, açıklama, uzmanlık, bütçe, embedded test sonucu), Onayla / Reddet (sebep) butonları, toplu onay.

### 1.2 Uzman Profil Onayı (expert)

Mevcut şemada `status: 'pending' | 'approved' | 'rejected'` alanı zaten var (`expert.schema.ts`) — admin paneline bağlanır.

| Kural | Açıklama |
|---|---|
| Onboarding | Uzman onboarding'i tamamlar → `pending`. `approved` olmadan: profili `/expert/[id]`'de görünmez, teklif gönderemez, Fırsatlar feed'ini göremez (veya salt-okunur görür — UX kararı). |
| Belge inceleme | Admin, CV / diploma / sertifika / lisans belgelerini panelden görüntüler (dosya önizleme). |
| Onay | `approved` → `isVerified` rozeti opsiyonel olarak ayrıca verilir (onay ≠ doğrulama rozeti; iki ayrı alan). |
| Red | `rejected` + sebep; uzman eksikleri giderip yeniden başvurur. |
| Kritik alan güncellemesi | Uzman **unvan, uzmanlık alanları, bio, belgeler** alanlarını güncellerse profil yeniden `pending` olur; eski onaylı sürüm yayında kalır, yeni sürüm onay bekler (versiyonlu onay). Avatar/iletişim gibi alanlar onay gerektirmez. |

Admin panel ekranı: bekleyen uzmanlar kuyruğu, belge görüntüleyici, alan bazlı diff (güncelleme onayında eski/yeni karşılaştırma), Onayla / Reddet / Rozet ver-al.

### 1.3 Yorum Onayı (review)

Yeni alan: `status: 'pending' | 'approved' | 'rejected'` (reviews tablosuna eklenir).

| Kural | Açıklama |
|---|---|
| Oluşturma | Danışan yorum bırakır → `pending`. Uzman profilinde görünmez. |
| Rating etkisi | **Sadece `approved` yorumlar** uzmanın `rating` ve `reviewCount` hesabına girer (hesap backend'de: onaylı yorumların ortalaması, 1 ondalık). |
| Red | `rejected` + sebep (hakaret, kişisel veri, spam vb. hazır sebep listesi + serbest metin). |
| Sonradan kaldırma | Admin, yayındaki (`approved`) bir yorumu sonradan `rejected`'a çekebilir → rating yeniden hesaplanır. |

> **Teklifler (offer) onay akışına GİRMEZ** — uzman teklif gönderdiğinde doğrudan danışana görünür (uzman zaten onaylı olduğu için). Admin yine de tüm teklifleri izleyebilir ve gerekirse iptal edebilir (bkz. 2.5).

---

## 2. DOMAIN YÖNETİMİ (CRUD + Durum Müdahalesi)

Genel kural: her varlık için **listeleme (filtre + arama + sıralama + sayfalama), detay, düzenleme, durum değiştirme (admin state machine'i override edebilir), silme/askıya alma** ve tüm işlemlerin **audit log**'a yazılması.

### 2.1 Kullanıcılar (auth / users)
- Tüm hesapları listele: rol (expert/client), email, kayıt tarihi, son giriş, durum.
- Hesap dondurma / aktifleştirme (dondurulan kullanıcı 403 alır).
- Hesap silme (soft delete — ilişkili ilan/teklif/eşleşme geçmişi korunur, kişisel veri anonimleştirilir / KVKK).
- Şifre sıfırlama maili tetikleme.
- Email doğrulama durumunu görüntüleme / manuel doğrulama.
- Kullanıcı adına oturum görüntüleme (impersonate — salt okunur, destek amaçlı; opsiyonel).

### 2.2 Uzmanlar (experts)
- Onay kuyruğu (bkz. 1.2) + tüm uzman listesi (status, rating, teklif/eşleşme sayıları).
- Profil alanlarını admin düzenleyebilir: unvan, uzmanlıklar, deneyim yılı, bio, eğitim, web sitesi.
- `acceptsOffers` aç/kapat (uzmanı geçici olarak feed'den düşürmeden teklif göndermesini durdurma).
- `isVerified` rozeti ver/al.
- Belge arşivi (yüklenen tüm CV/sertifikalar, tarihçeli).
- Uzmanın tüm teklifleri, eşleşmeleri, yorumları tek ekrandan.

### 2.3 Danışanlar (clients)
- Danışan listesi: fullName, email, telefon, `registrationType` (invited/self), `matchStatus` (FREE/PENDING/MATCHED/RELEASED), matchCode.
- Profil ve notları düzenleme.
- Danışanın ilanları, kabul ettiği teklifler, eşleşmeleri, test sonuçları tek ekrandan.
- Faz 1 desteği: uzman adına manuel danışan ekleme + davet gönderimini tetikleme/yineleme.

### 2.4 İlanlar (listings)
- Onay kuyruğu (bkz. 1.1) + tüm ilanlar (statü filtreli).
- İçerik düzenleme (başlık/açıklama/uzmanlık/bütçe — düzeltme amaçlı).
- Manuel durum değiştirme: kapatma (`CLOSED`), süre uzatma (`expiresAt` güncelleme), süresi dolmuşu yeniden açma (istisnai — normal kural: MATCHED/EXPIRED yeniden açılamaz, admin override loglanır).
- İlanın aldığı teklifleri ve görüntülenme sayısını izleme.

### 2.5 Teklifler (offers)
- Tüm teklifler: PENDING / ACCEPTED / REJECTED / WITHDRAWN filtreleri, uzman/ilan bazlı arama.
- Admin müdahalesi: PENDING teklifi iptal etme (uzman adına WITHDRAWN — kural ihlali/şikayet durumunda).
- Kural izleme: "bir uzman aynı ilana 1 teklif" ihlal denemeleri raporu.

### 2.6 Eşleşmeler (matches)
- Tüm eşleşmeler: ACTIVE / COMPLETED / RELEASED, taraflar, ilan + kabul edilen teklif referansı.
- Manuel sonlandırma: normalde RELEASED iki taraf onayı ister; **ihtilaf durumunda admin tek taraflı RELEASED/COMPLETED yapabilir** (sebep zorunlu, loglanır).
- İhtilaf/şikayet kaydı: eşleşmeye not ekleme, taraflarla yazışma geçmişi (opsiyonel destek modülü).

### 2.7 Ödemeler (payments / wallet)
- Paket satın alımları: kim, hangi paket, tutar, Iyzico işlem no, 3D Secure sonucu.
- Cüzdan hareketleri (credit/debit) görüntüleme + **manuel düzeltme kaydı** ekleme (iade, jest, hata düzeltme — çift onay önerilir).
- İade tetikleme: paket süresi dolduğunda kullanılmayan seansların otomatik iade kuyruğu + manuel iade.
- Satın alınan paketlerin durumu: usedSessions / kalan / expiresAt; admin süre uzatabilir.

### 2.8 Bildirimler (notifications)
- Gönderilen bildirimlerin logu (tip, alıcı, okundu bilgisi).
- Manuel bildirim gönderme: tek kullanıcıya / segmente (tüm uzmanlar, tüm danışanlar, şehir/uzmanlık filtresi) — tip `SYSTEM`.
- Rate limit ayarları (bkz. 5).

---

## 3. İÇERİK YÖNETİMİ (CMS)

Kaynak tablolar `mock-db/data/*.json` — hepsi admin panelden tam CRUD.

### 3.1 Günün Önerisi / Öneriler (`suggestions`)
| Alan | Açıklama |
|---|---|
| audience | `client` / `expert` / `all` — hangi rolün ana sayfasında dönecek |
| category | Serbest etiket: "Günün Önerisi", "Uyku", "Farkındalık", "İpucu"… |
| title, body | İçerik |
| **isActive** (yeni) | Yayında/pasif |
| **sortOrder / dateRange** (yeni) | Sıralama veya tarih bazlı gösterim (gerçek "günün" önerisi için) |

### 3.2 Kategoriler / Hastalık Çeşitleri (`categories`)
- 17 mevcut kayıt (Anksiyete, Depresyon, OKB, Travma & PTSD…). Alanlar: slug, name, icon (Lucide adı), summary, description, blogTag, assessmentCategory.
- Admin: ekleme/düzenleme/pasife alma (silme yerine — ilan ve blog ilişkileri bozulmasın), sıralama.
- **Kritik:** Bu tablo tek kaynak olacak — `SPECIALIZATION_OPTIONS` (ilan formu chip'leri) ve uzman onboarding uzmanlık listesi de buradan beslenir (bkz. 4). Kategori eklemek = ilan formunda ve uzman profillerinde otomatik seçilebilir olması.
- `expertCount`, `completedMatchCount` backend hesaplar (admin salt izler).

### 3.3 Blog (`blogs`)
- Alanlar: slug, title, excerpt, content (markdown/rich text), coverImage (upload), categories (→ kategori tablosuna ilişki: **hangi hastalıkla ilgili**), author {name, title}, readingTime, publishedAt.
- Yeni alanlar: **status: draft / published / archived**, plânlı yayın tarihi.
- likeCount salt izlenir; admin sahte beğeni ekleyemez.
- Rich text editör + görsel yükleme (Supabase Storage).

### 3.4 Testler (`assessments`) — sorular + SKORLAMA
Test yapısı: id, title, category (→ kategori ilişkisi), description, estimatedMinutes, questions[] {text, type: single_choice/multiple_choice/scale, options[] {text, **value**}}.

Admin panelden yönetilecekler:
- Test CRUD + soru/seçenek sıralama, seçenek puan değerleri.
- Test durumu: draft / published (yayında olmayan test uygulamada listelenmez).
- **Skorlama kuralları DB'ye taşınır** — şu an `mock-db/handlers/assessment.handlers.js` içinde hardcoded (toplam ≤4 → low, ≤9 → moderate, else high; tek tip kaygı metni tüm testlere uygulanıyor). Yeni model, **test başına**:

```
assessment_score_rules:
  assessmentId
  level        (low | moderate | high)
  minScore, maxScore
  summary      (sonuç özet metni — teste özgü)
  suggestions  (öneri listesi — teste özgü)
```

- Sonuç seviye etiketleri ("Düşük/Orta/Yüksek") ve seviye başına uzman yönlendirme CTA metni de admin'den düzenlenebilir.
- Gönderilen test sonuçları (assessment-results) salt-okunur liste: skor dağılımı, email lead'leri (opsiyonel bırakılan emailler → pazarlama listesi ihracı).

### 3.5 Paketler (`packages`)
- Alanlar: name, sessionCount, price, unitPrice, discountPct, validDays, isPopular + **isActive** (yeni).
- Backend validasyonu (admin formunda da zorlanır): `3 ≤ sessionCount ≤ 20`, `price ≤ unitPrice × sessionCount` (indirim şart), `originalPrice` backend hesaplar.
- `isPopular` rozeti tek pakette olabilir (öneri).
- Fiyat değişikliği geçmişi loglanır; satın alınmış paketler eski koşullarıyla devam eder.

### 3.6 Bildirim Şablonları (yeni tablo: `notification_templates`)
Şu an bildirim gövdeleri backend'de hazır string olarak üretiliyor — şablonlaştırılır:

| type | Değişkenler | Örnek |
|---|---|---|
| OFFER_RECEIVED | {expertTitle}, {listingTitle}, {price} | "İlanınıza yeni teklif geldi" |
| OFFER_ACCEPTED | {clientName}, {listingTitle} | "Teklifiniz kabul edildi" |
| LISTING_EXPIRING | {listingTitle}, {daysLeft} | "İlanınızın süresi doluyor" |
| LISTING_APPROVED / LISTING_REJECTED (yeni) | {listingTitle}, {reason} | Onay akışı bildirimleri |
| EXPERT_APPROVED / EXPERT_REJECTED (yeni) | {reason} | Uzman onay bildirimleri |
| REVIEW_APPROVED / REVIEW_REJECTED (yeni) | {reason} | Yorum onay bildirimleri |
| SYSTEM | serbest | Manuel duyurular |

Her şablon: title, body, kanal bayrakları (push / in-app / SMS), aktif/pasif.

---

## 4. UI METİNLERİNİN DB'YE TAŞINMASI (Frontend Değişiklik Listesi)

Karar: hardcoded UI içerikleri kodda kalmayacak; mock-db'ye yeni koleksiyonlar olarak taşınacak (mock-db = prod DB şeması, sonra Supabase'e convert). Admin panelden düzenlenebilir olacaklar:

| İçerik | Şu anki yeri | Hedef |
|---|---|---|
| Welcome slaytları (3 slayt: ikon, başlık, gövde, CTA etiketleri) | `src/app/(auth)/welcome.tsx` (`FIRST_SLIDE`, `SLIDES`) | yeni `ui-content.json` → `ui_content` tablosu (key-value + JSON payload) |
| Home hero başlık/alt başlık ("Bugün nasıl hissediyorsunuz?" vb.) | `ClientHomeScreen.tsx`, `ExpertHomeScreen.tsx` | `ui_content` |
| Quick-action etiketleri (Testlerim, Fırsatlar…) | Home screen'ler | `ui_content` |
| `SPECIALIZATION_OPTIONS` (17 uzmanlık — kategorilerin kopyası) | `src/domains/listing/listing.constants.ts` | **kaldırılır**, `categories` API'sinden beslenir |
| Uzman onboarding uzmanlık listesi (`ExpertSpecializations`) | `expert.schema.ts` | `categories` API'sinden |
| `SESSION_TYPE_LABELS` (Online / Yüz yüze / Fark etmez) | `listing.constants.ts` | `ui_content` |
| Status etiket METİNLERİ (`LISTING/OFFER/MATCH_STATUS_CONFIG`, `RESULT_LEVEL_CONFIG`, `NOTIFICATION_TYPE_CONFIG` içindeki `label` alanları) | domain `*.constants.ts` dosyaları | `ui_content` (ikon/renk kodda kalır) |
| Onboarding adım başlık/placeholder metinleri | `src/domains/expert/components/onboarding/*`, `create-listing/*` | `ui_content` |
| Profil menü etiketleri | `src/app/(tabs)/profile.tsx` | `ui_content` |
| Teklif kabul uyarı metni ("İletişim bilgileriniz paylaşılacaktır") | `OfferCard` Alert | `ui_content` |

Önerilen `ui_content` şeması: `{ key: string (örn. "home.client.heroSubtitle"), value: string | json, audience?: client/expert/all, updatedAt }` — uygulama açılışta toplu çeker (`GET /ui-content`), 10 dk cache.

> Bu taşıma ayrı bir frontend refactor işidir; bu doküman kapsamı gereksinim tanımıdır.

---

## 5. SİSTEM PARAMETRELERİ (yeni tablo: `system_settings`)

Kodda sabit olan iş kuralları admin'den ayarlanabilir olur (key-value, tip ve min/max validasyonlu):

| Parametre | Mevcut değer | Yeri |
|---|---|---|
| LISTING_MAX_ACTIVE | 3 | `listing.constants.ts` |
| LISTING_EXPIRE_DAYS | 30 | `listing.constants.ts` |
| PACKAGE_VALID_DAYS | 180 | `payment.constants.ts` |
| PACKAGE_MIN_SESSIONS / MAX_SESSIONS | 3 / 20 | iş kuralı |
| NOTIFICATION_RATE_LIMIT | — | bildirim başına saatlik/günlük limit |
| LISTING_AUTO_APPROVE | false | onay akışını acil durumda bypass etme anahtarı |
| MAINTENANCE_MODE + mesajı | false | uygulamayı bakım moduna alma |

Değişiklikler audit log'a yazılır ve mevcut kayıtları geriye dönük etkilemez (örn. expire süresi değişse yayındaki ilanların `expiresAt`'i değişmez).

---

## 6. DASHBOARD & İZLEME

- **Bekleyen onay sayaçları:** ilan / uzman / yorum (panelin ana ekranı, tıklayınca kuyruğa gider).
- **Günlük metrikler:** yeni kayıt (rol bazlı), açılan ilan, gönderilen teklif, oluşan eşleşme, çözülen test, blog okuma/beğeni.
- **Dönüşüm hunisi:** ilan → teklif → kabul → eşleşme oranları; ortalama ilk teklif süresi.
- **Gelir raporu:** paket satışları (günlük/aylık), iade tutarları, Iyzico başarısız işlem oranı.
- **Audit log:** hangi admin, hangi kaydı, ne zaman, hangi alanları (eski→yeni değer) değiştirdi — tüm admin yazma işlemleri için zorunlu, silinemez.

---

## 7. ADMIN ROLLERİ (öneri)

| Rol | Yetki |
|---|---|
| Süper Admin | Her şey + admin kullanıcı yönetimi + sistem parametreleri + cüzdan düzeltme |
| Moderatör | Onay kuyrukları (ilan/uzman/yorum), kullanıcı dondurma, eşleşme müdahalesi |
| İçerik Editörü | CMS: blog, kategori, öneri, test, ui_content, bildirim şablonları |
| Finans | Ödemeler, iadeler, paket yönetimi, gelir raporları |

---

## 8. SUPABASE ŞEMA KARŞILIĞI

Her mock-db JSON dosyası bir tabloya karşılık gelir; onay akışları için eklenen kolonlar:

| Tablo (kaynak JSON) | Eklenen kolonlar |
|---|---|
| listings | `status`'a `PENDING_APPROVAL`, `REJECTED_BY_ADMIN` değerleri; `rejectionReason`, `approvedAt`, `approvedBy` |
| experts | (status zaten var) `rejectionReason`, `approvedAt`, `approvedBy`, `pendingRevision` (versiyonlu onay için JSON) |
| reviews | `status`, `rejectionReason`, `moderatedAt`, `moderatedBy` |
| blogs | `status` (draft/published/archived), `scheduledAt` |
| assessments | `status` (draft/published) |
| packages | `isActive` |
| suggestions | `isActive`, `sortOrder` |
| **Yeni tablolar** | `assessment_score_rules`, `notification_templates`, `ui_content`, `system_settings`, `audit_logs`, `admin_users` (+roller) |

Diğer tablolar (auth/users, clients, matches, offers, notifications, wallet, assessment-results) mevcut şemayla taşınır.

---

*Son güncelleme: 24 Temmuz 2026 — Tekin Labs (v1: admin panel gereksinim envanteri)*
