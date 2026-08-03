# PsikoAl — Gerçek Backend + Admin Panel Yol Haritası

## Context

Expo frontend (native-atomic) tamamen mock API (mock-db) üzerinde çalışıyor; tüm hook'lar gerçek REST sözleşmesine Zod doğrulamalı yazılmış. Şimdi gerçek altyapı kurulacak: **Supabase** (Postgres + Auth + Roles + Storage + e-posta) ve **C# backend + Blazor Server admin panel** (`csharp-clean-code` skill'i standartlarıyla). Prensip: Supabase'in etinden sütünden faydalan (RLS, trigger, DB fonksiyonları, transaction, storage, e-posta) — ama hesaplamalar C# backend'de; frontend hiçbir şey hesaplamaz. Landing page en sona (kapsam dışı).

Sözleşme kaynakları: `docs/BACKEND_REQUIREMENTS.md`, `docs/ADMIN_PANEL_REQUIREMENTS.md` (her `mock-db/data/*.json` = bir tablo + yeni tablolar), davranış referansı `mock-db/handlers/`, contract kaynağı `native-atomic/src/domains/*/schemas/` (Zod).

## Verilen Kararlar

| Karar | Seçim |
|---|---|
| Frontend auth | **C# proxy**: frontend mevcut `/auth/*` endpoint'lerini aynen kullanır; C# arkada Supabase Auth (GoTrue Admin API) çağırır, Supabase JWT üretir/yeniler. Frontend auth domain'i değişmez. |
| Admin panel | **Blazor Server** — aynı solution'da, Application katmanını doğrudan kullanır |
| Repo düzeni | **Ayrı repo `psikoal-backend`**: `/supabase` (SQL migration'lar) + `/src` (C# solution) + CI. Frontend `psikoal-app` reposunda kalır. |
| İlk dilim | **Auth + Profiller** (en riskli entegrasyon en önce) |
| DB erişimi | **EF Core (Npgsql)** ORM — sorgu/transaction için. Şema kaynağı **Supabase CLI SQL migration'ları** (RLS, trigger, fonksiyon SQL'de birinci sınıf); EF migrations kapalı, entity'ler elle/scaffold. Entity↔şema sapması CI'da scaffold-diff uyum testiyle yakalanır. *(Kullanıcıyla konuşuldu ve onaylandı.)* |
| Geliştirme kolaylığı | **Supabase MCP kurulumu önerilir** (kullanıcı kuracak) — migration uygulama, tablo inceleme, SQL çalıştırmayı benim doğrudan yapabilmem için. ORM zaten EF Core; MCP ek geliştirme konforu. |

## Postgres ↔ C# sınırı

- **Postgres'te**: çok tablolu atomik değişimler (örn. `accept_offer()` DB fonksiyonu: teklif kabul + diğer PENDING'ler REJECTED + listing→MATCHED + match insert, tek transaction), CHECK/UNIQUE constraint'ler ("aynı ilana 1 teklif", LISTING_MAX_ACTIVE), RLS politikaları, `pg_cron` ile ilan expiry (30 gün), rating agregasyonu (onaylı yorumlardan view/fonksiyon), `auth.users` insert trigger'ı ile profil satırı.
- **C#'ta**: response envelope + computed sunum alanları (maskeleme `"Zeynep Y."`, `initials`, `budgetLabel`, `timeLabel`/relative time, meta sayaçları), assessment skorlama (`assessment_score_rules` tablosundan), Iyzico entegrasyonu + webhook, şablonlu bildirim/e-posta orkestrasyonu, audit log yazımı, admin iş akışları.

## Fazlar

### Phase 0 — Temel Mimari (Foundation) → Review 0
1. GitHub repo: `psikoal-backend` (kullanıcı `gh` ile açar/açtırır). Yapı: `/supabase/migrations`, `/src`, `/.github/workflows`.
2. Supabase projesi + Storage bucket'ları (`avatars`, `documents`, `blog-media`) — kullanıcı Supabase hesabında oluşturur; ben SQL/policy'leri yazarım.
3. **`csharp-clean-code` skill'i yüklenerek** solution iskeleti: `PsikoAl.Domain` / `PsikoAl.Application` / `PsikoAl.Infrastructure` (EF Core + Supabase servisleri) / `PsikoAl.Api` (REST) / `PsikoAl.AdminWeb` (Blazor Server) + test projeleri.
4. `0001_init` migration: `system_settings`, `audit_logs`, `admin_users` + ortak enum/extension'lar.
5. Api tarafında Supabase JWT doğrulama + `/auth/login|register|refresh` proxy'sinin iskeleti + `whoami` ucu (uçtan uca token akışı kanıtı).
6. CI: build + test + migration lint. Contract-test iskeleti: frontend Zod şemalarından türetilen yanıt doğrulaması (her dilimin geçiş kriteri).

### Feature Dilimleri (her biri: **DB migration → C# API → Admin panel → Frontend bağlama → Review**)
1. **Auth + Profiller** — users/profiles tabloları, RLS, `/auth/*` proxy tam seti (login, register, refresh, freeze, delete, change/forgot password, PATCH me), avatar upload (Storage), Admin: kullanıcı listesi + dondurma/soft delete (KVKK anonimleştirme). Frontend: `EXPO_PUBLIC_API_URL` gerçek API'ye, mock kapalı smoke test.
2. **Uzman onboarding + versiyonlu onay** — experts tablosu + `pendingRevision`, belge yükleme (CV/sertifika → Storage; frontend'deki "yakında" stub'ları gerçeğe bağlanır), Admin: onay kuyruğu + belge görüntüleyici + alan diff + isVerified/acceptsOffers.
3. **Kategoriler + Keşfet + Yorum moderasyonu** — categories (17 kayıt, tek kaynak), reviews + `status` + rating recalc, Admin: kategori CRUD + yorum kuyruğu.
4. **İlan akışı + onay** — listings + `PENDING_APPROVAL→OPEN→MATCHED→CLOSED/EXPIRED` state machine, pg_cron expiry, LISTING_MAX_ACTIVE constraint, feed filtre/sıralama, Admin: ilan onay kuyruğu + system_settings ekranı. Frontend: `PENDING_APPROVAL/REJECTED_BY_ADMIN` durumları şemalara eklenir.
5. **Teklif + Eşleşme** — offers/matches, `accept_offer()` DB fonksiyonu (tek transaction), iki taraflı release onayı (`clientReleasedAt`/`expertReleasedAt` — mock'taki tek taraflı davranış düzeltilir), eşleşme öncesi ad maskeleme (uzmana tam ad hiç gönderilmez), Admin: teklif iptal + ihtilaf çözümü (zorla RELEASED/COMPLETED).
6. **Bildirimler + e-posta** — notifications(+userId), `notification_templates`, olay tabanlı üretim (outbox pattern: offer accept vb. dilim-5 olayları burada bağlanır), Supabase e-posta + Expo push kaydı, Admin: şablon CRUD + manuel/segment gönderim.
7. **Assessment** — assessments + `assessment_score_rules` (skorlama DB kuralından C#'ta), sonuçlar kullanıcıya bağlı, Admin: test/soru/puan kuralı CRUD.
8. **Ödeme** — packages + wallet + transactions, Iyzico 3DS init + webhook (idempotency), atomik bakiye hareketi, Admin: işlem izleme + manuel düzeltme (çift onay) + iade kuyruğu.
9. **İçerik** — blogs (draft/published/scheduled, Storage görselleri), suggestions, `ui_content` (+ `GET /ui-content`, frontend'e cache'li bağlama).
10. **Dashboard + sertleştirme** — admin dashboard (onay sayaçları, huni, gelir), audit log görüntüleyici, RLS/negatif test taraması, `PRODUCTION_CHECKLIST.md` uygulanması (mock söküm), prod yayın. *(Landing page bundan sonra ayrı iş.)*

Her dilim sonunda **Review checkpoint**: contract testleri yeşil + frontend'de ilgili ekranların gerçek API ile smoke testi + kullanıcı onayı alınmadan sonraki dilime geçilmez.

## Riskler
- **Auth üçgeni** (Expo ↔ C# ↔ Supabase JWT/refresh) → dilim 1'de öne alındı; proxy'de refresh kuyruğu davranışı frontend interceptor'ıyla birebir test edilir.
- **service_role RLS baypası** → backend service_role kullanacaksa RLS'e ayrıca negatif testler yazılır.
- **Mock ↔ gerçek sözleşme sapması** → her dilimde Zod contract testi geçiş kriteri.
- **Offer-accept yarış durumu** → DB fonksiyonu + row lock ile çözülür.
- **Iyzico webhook güvenilirliği** → idempotency key + işlem logu.
- **pg_cron plan kısıtı** → alternatif: Supabase Edge Function scheduled / hosted C# background service.

## Kullanıcının Kuracakları (benim yazacağım talimatlarla)
- GitHub `psikoal-backend` reposu, Supabase projesi + API anahtarları, **Supabase MCP** (geliştirme konforu için — önerim bu; ORM olarak zaten EF Core kullanılacak), Iyzico sandbox hesabı (dilim 8'den önce).

## Doğrulama
Her dilimde: (1) `dotnet build` + unit testler, (2) migration'ın temiz Supabase'e uygulanabilirliği, (3) Zod contract testleri, (4) frontend'in ilgili ekranlarının gerçek API ile mock kapalıyken smoke testi, (5) admin panelde ilgili akışın elle doğrulanması.
