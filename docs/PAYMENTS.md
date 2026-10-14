# Ödemeler — Uzman Kredisi (Armut modeli)

> Kanonik kaynak. Ürün kararları 2026-10-14. Eski danışan ödeme modeli (cüzdan, seans
> paketi, Iyzico 3D Secure) **tamamen terk edildi**; ilgili eski metinler bu belgeye yönlenir.

## Model

- **Danışandan para alınmaz.** Uzman, bir ilana **teklif gönderirken** kredi harcar
  (`OFFER_CREDIT_COST`, varsayılan 1, admin panelinde Ayarlar'dan değişir; 0 = ücretsiz).
- Harcanan kredi **iade edilmez** (teklif reddedilse, geri çekilse, ilan kapansa da).
  Gerekirse finans yetkilisi elle düzeltir (gerekçe zorunlu, denetim kaydına yazılır).
- Yeni uzmana başlangıç kredisi **yok**.
- Uzman–danışan arasındaki seans ödemesi platformu ilgilendirmez.
- Kredi **yalnız web sitesinden** satın alınır (ileride ana alan adında). Sağlayıcı:
  **PayTR iFrame API**.

## App Store / Google Play kuralı

Mobil uygulamada satın alma butonu, ödeme sayfasına bağlantı, fiyat ya da "web'den
alırsanız ucuz" yönlendirmesi **yoktur**. Uygulama yalnız şunları gösterir:

- bakiye (`GET /credits/me` → `balanceLabel`),
- kredi geçmişi (`GET /credits/transactions`),
- teklif maliyeti (`offerCost`),
- düz metin: **"Kredi yüklemek için web sitemizi ziyaret edin."** (tıklanabilir bağlantı değil).

Bakiye yetmezse `POST /offers` 422 `INSUFFICIENT_CREDITS` döner; uygulama yalnız mesajı gösterir.

## Veri

| Tablo | Not |
|---|---|
| `experts.credit_balance` | Tek satırda bakiye, `CHECK (credit_balance >= 0)`; yalnız koşullu UPDATE ile değişir |
| `credit_packages` | `name`, `credits > 0`, `price_try > 0`, `is_active`, `sort_order` |
| `credit_payments` | `merchant_oid` (UNIQUE, yalnız harf+rakam), `amount_try`, `credits`, `status` `PENDING \| PAID \| FAILED`, `provider_payload` (hash/kart bilgisi yok) |
| `credit_transactions` | Defter, append-only: `amount (+/-)`, `balance_after`, `type` `PURCHASE \| OFFER_SPEND \| ADMIN_ADJUST`, `offer_id?`, `payment_id?`, `admin_user_id?`, `reason?` |
| `system_settings.OFFER_CREDIT_COST` | Tam sayı, 0–1000 |

Migration: `supabase/migrations/20261015100000_expert_credits.sql`.

## Uçlar

Tüm yanıtlar `{ data }` zarfında; hata gövdesi her yerde `{ code, message, field? }`.

### Uzman (mobil uygulama) — `Authorization: Bearer <token>`

| Uç | Yanıt |
|---|---|
| `GET /credits/me` | `{ data: { balance: int, offerCost: int, balanceLabel: "12 kredi" } }` |
| `GET /credits/transactions?page=1&perPage=20` | `{ data: [{ id, amount, balanceAfter, type, typeLabel, description, offerId \| null, createdAt, timeLabel }], meta: { page, total, perPage } }` — `perPage` en çok 50, en yeni önce |

`typeLabel`: `Kredi yükleme`, `Teklif gönderimi`, `Yönetici düzeltmesi`. `description`
Türkçe hazır metindir (`"X" ilanına teklif gönderildi`, `Başlangıç paketi satın alındı`,
`Yönetici düzeltmesi: <gerekçe>`). `timeLabel` göreli zaman (`2 saat önce`).

### Web sitesi ödeme akışı

| Uç | Yetki | Gövde / yanıt |
|---|---|---|
| `GET /credits/packages` | anonim | `{ data: [{ id, name, credits, priceTry, priceLabel: "500,00 ₺", unitPriceLabel: "25,00 ₺ / kredi" }] }` — yalnız aktif, `sort_order` sırasıyla |
| `POST /payments/paytr/init` | Bearer, **onaylı** uzman | gövde `{ packageId }` → `{ data: { iframeToken, merchantOid, iframeUrl: "https://www.paytr.com/odeme/guvenli/<token>" } }` |
| `POST /payments/paytr/callback` | anonim, PayTR sunucusu | `application/x-www-form-urlencoded`; yanıt düz metin `OK` |

Web sitesinin yapacakları:

1. Uzman web sitesinde **aynı hesapla** giriş yapar (`POST /auth/login`, mobil ile aynı API).
2. `GET /credits/packages` ile paketleri listeler.
3. Seçilen paket için `POST /payments/paytr/init` çağrısını **tarayıcıdan** yapar.
   PayTR `user_ip` alanına kullanıcının gerçek IP'sini ister; API bunu `X-Forwarded-For`
   üzerinden okur. Çağrı web sitesinin kendi sunucusundan (BFF) yapılırsa PayTR'ye o
   sunucunun IP'si gider — o durumda sunucu, kullanıcının IP'sini `X-Forwarded-For` ile
   iletmeli ve API'nin `ForwardedHeaders__KnownProxies` listesine eklenmelidir.
4. Dönen `iframeUrl`'i bir `<iframe>` içinde açar (PayTR'nin `iframeResizer.min.js`
   betiği isteğe bağlı).
5. Kullanıcı ödeme sonrası `PayTR__OkUrl` / `PayTR__FailUrl` sayfasına döner. **Bu
   yönlendirme ödemenin kanıtı değildir**: kredi yalnız PayTR'nin sunucudan sunucuya
   bildirimiyle (callback) yüklenir. Başarı sayfası bakiyeyi `GET /credits/me` ile birkaç
   saniye aralıkla yoklayarak gösterir.

Kredi yüklendiğinde uzmana uygulama içi + push bildirimi gider (`CREDIT_PURCHASED`,
"Krediniz yüklendi"; e-posta kapalı). Şablon admin panelinden düzenlenir.

### Callback davranışı

- Hash: `base64(HMAC-SHA256(merchant_oid + merchant_salt + status + total_amount, merchant_key))`.
  Geçersizse 400 `INVALID_HASH` (düz metin) — `OK` **dönmez**, PayTR panelde başarısız görünür.
- Bilinmeyen `merchant_oid` → 404 `UNKNOWN_ORDER` (log'a hata düşer).
- `status=success` ve `total_amount >= ödeme tutarı (kuruş)` → `PENDING → PAID`, kredi +
  `PURCHASE` defter satırı + bildirim, tek transaction. Taksit/vade farkıyla `total_amount`
  büyük olabilir; küçükse ödeme `FAILED` sayılır ve log'a hata düşer.
- `status=failed` → `FAILED`, kredi yok.
- **Idempotent:** PayTR aynı bildirimi birden fazla gönderebilir. `PENDING`'den çıkış koşullu
  UPDATE'tir; ikinci (ya da eşzamanlı) bildirim krediyi tekrar yüklemez ve yine `OK` alır.
  Veritabanında ayrıca `payment_id` başına tek `PURCHASE` satırı benzersiz indeksle korunur.

Kaynak: PayTR iFrame API [1. adım](https://dev.paytr.com/iframe-api/iframe-api-1-adim),
[2. adım (Bildirim URL)](https://dev.paytr.com/iframe-api/iframe-api-2-adim).

### Admin (finans) — `AdminFinance` policy: `super_admin`, `finance`

| Uç | Not |
|---|---|
| `GET /admin/credits/packages` | Tüm paketler + `paidPurchaseCount` |
| `POST /admin/credits/packages` | `{ name, credits, priceTry, isActive, sortOrder }` |
| `PUT /admin/credits/packages/{id}` | Aynı gövde |
| `DELETE /admin/credits/packages/{id}` | Satın alma geçmişi varsa 409 `CREDIT_PACKAGE_IN_USE` — pasife alın |
| `GET /admin/credits/payments?status=&page=&perPage=` | Sayfalı satın almalar |
| `GET /admin/credits/experts/{expertId}?page=&perPage=` | `{ expertId, fullName, balance, transactions: { data, meta } }` |
| `POST /admin/credits/experts/{expertId}/adjust` | `{ amount (≠0, ±10000), reason }` → aynı yanıt; bakiye eksiye düşerse 422 `CREDIT_BALANCE_NEGATIVE` |

Tüm yazma işlemleri `audit_logs`'a düşer (`admin.credit_package_*`, `admin.credit_adjust`).
`OFFER_CREDIT_COST` `PATCH /admin/system-settings/OFFER_CREDIT_COST` (yalnız `super_admin`).

## Teklif gönderimi (atomiklik)

`POST /offers` mevcut kuralları (uzman onaylı, ilan açık, aynı ilana tek teklif) **önce**
çalıştırır; ihlalde kredi düşmez. Sonra tek transaction'da:

```sql
update experts set credit_balance = credit_balance - @cost
 where id = @expert and credit_balance - @cost >= 0
returning credit_balance;   -- satır yoksa 422 INSUFFICIENT_CREDITS
```

+ teklif insert + `OFFER_SPEND` defter satırı. Teklif yazılamazsa (ör. eşzamanlı ikinci
teklif benzersizlik kısıtına takılırsa) düşüm geri alınır. Satır kilidi aynı uzmanın
eşzamanlı tekliflerini sıraya sokar.

## Hata kodları

| Kod | HTTP | Mesaj |
|---|---|---|
| `INSUFFICIENT_CREDITS` | 422 | Teklif göndermek için yeterli krediniz yok. |
| `CREDIT_PACKAGE_NOT_FOUND` | 404 | Kredi paketi bulunamadı. |
| `CREDIT_PACKAGE_IN_USE` | 409 | Bu paketle yapılmış satın almalar var; silmek yerine pasif hâle getirin. |
| `CREDIT_BALANCE_NEGATIVE` | 422 | Bu düzeltme uzmanın bakiyesini eksiye düşürür. |
| `CREDIT_ADJUST_REASON_REQUIRED` | 422 | Kredi düzeltmesi için gerekçe yazmanız gerekiyor. (HTTP'de gövde doğrulaması önce 400 döner; kod servis katmanının güvencesidir) |
| `CREDIT_PURCHASE_EXPERT_NOT_APPROVED` | 422 | Kredi satın almak için uzman profilinizin onaylanması gerekiyor. |
| `PAYMENT_PROVIDER_UNAVAILABLE` | 422 | Ödeme servisine şu an ulaşılamıyor. Biraz sonra tekrar deneyin. |
| `EXPERT_ROLE_REQUIRED` | 403 | Danışan hesabı `/credits/me` ya da ödeme başlatma çağırırsa |

Gövde doğrulama hataları (boş `packageId`, eksik gerekçe vb.) mevcut kural gereği 400
ValidationProblemDetails döner. Rate limit: `init` kullanıcı başına 10/dk, `callback` IP
başına 300/dk (429).

## Yapılandırma

| Anahtar | Not |
|---|---|
| `PayTR__MerchantId` | Mağaza No |
| `PayTR__MerchantKey` | Mağaza Parola — gizli, yalnız ortam değişkeni |
| `PayTR__MerchantSalt` | Mağaza Gizli Anahtar — gizli, yalnız ortam değişkeni |
| `PayTR__TestMode` | `true` → PayTR `test_mode=1` (+ `debug_on=1`) |
| `PayTR__OkUrl` / `PayTR__FailUrl` | Web sitesinin dönüş sayfaları (tam adres) |

Anahtarlar appsettings dosyalarına yazılmaz. Üçü de boşsa ödeme uçları
`PAYMENT_PROVIDER_UNAVAILABLE` döner, callback her bildirimi reddeder. Render kurulumu:
`docs/DEPLOY.md`.

## PayTR Mağaza Paneli ayarları

1. **Bildirim URL'si** (Destek & Kurulum → Ayarlar → Bildirim URL):
   `https://<api-alan-adı>/payments/paytr/callback` — ör.
   `https://psikoal-api.onrender.com/payments/paytr/callback`. HTTPS zorunlu; adres oturum
   ya da giriş istememeli (uç anonimdir).
2. Entegrasyon bilgileri (Mağaza No / Parola / Gizli Anahtar) → Render ortam değişkenleri.
3. **Test modu:** canlı mağazada `PayTR__TestMode=true` iken PayTR'nin test kartlarıyla
   uçtan uca deneme yapılır; canlı satışa geçerken `false`.
4. `merchant_ok_url` / `merchant_fail_url` her istekte API'den gider (`PayTR__OkUrl`,
   `PayTR__FailUrl`); panelde ayrıca girilmez.
5. Taksit kapalı gönderilir (`no_installment=1`).
