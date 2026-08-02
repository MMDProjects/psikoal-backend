# Kurulum — Senin Yapacakların

## 1. Supabase Projesi

1. https://supabase.com/dashboard → **New project** (ad: `psikoal`, bölge: `eu-central-1` önerilir, güçlü DB şifresi belirle ve sakla).
2. Proje açıldıktan sonra **Project Settings → API**'den şunları al:
   - `Project URL`
   - `anon public` key
   - `service_role` key (gizli!)

   (JWT doğrulaması Supabase'in yayınladığı JWKS/OIDC discovery ile yapılır — ayrıca "JWT Secret" almana gerek yok.)
3. **Database → Connection string** (URI, pooler/Session mode) → Postgres bağlantı dizesi.

Not: 3 Storage bucket'ı (`avatars` public-read, `blog-media` public-read, `documents` private) elle oluşturmana gerek yok — `supabase/migrations/00000000000002_storage_buckets.sql` ile (Supabase MCP üzerinden) otomatik kuruldu, RLS policy'leriyle birlikte.

## 2. Lokal Konfigürasyon

`src/PsikoAl.Api/appsettings.Local.json` oluştur (gitignore'da, commit edilmez):

```json
{
  "Supabase": {
    "Url": "https://XXXX.supabase.co",
    "AnonKey": "...",
    "ServiceRoleKey": "..."
  },
  "ConnectionStrings": {
    "Postgres": "Host=...;Database=postgres;Username=postgres;Password=...;SSL Mode=Require"
  }
}
```

## 3. Supabase MCP (önerilir — Claude'un DB'yi doğrudan yönetebilmesi için)

```bash
claude mcp add supabase -- npx -y @supabase/mcp-server-supabase --project-ref=XXXX
```

Erişim token'ı ister: https://supabase.com/dashboard/account/tokens → **Generate new token**.
Kurulunca Claude migration uygulama / tablo inceleme / SQL çalıştırmayı doğrudan yapabilir.

## 4. Migration Uygulama (MCP yoksa)

Supabase Dashboard → **SQL Editor** → `supabase/migrations/*.sql` dosyalarını sırayla çalıştır.
(İleride `supabase` CLI ile `supabase db push` akışına geçilecek.)

## 5. İleride Gerekecekler

- **Iyzico sandbox** hesabı (Dilim 8 — ödeme'den önce)
- SMTP / Supabase e-posta şablonları (Dilim 6 — bildirimlerden önce)
