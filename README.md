# PsikoAl Backend

PsikoAl pazar yerinin gerçek altyapısı: **Supabase** (Postgres + Auth + Storage + e-posta) ve **C# (.NET 10)** REST API + Blazor Server admin paneli.

## Yapı

```
supabase/migrations/   ← Şema kaynağı: SQL-first migration'lar (RLS, trigger, fonksiyonlar dahil)
src/
  PsikoAl.Common/      ← DTO'lar, ErrorKeys, exception'lar (hiçbir katmana bağımlı değil)
  PsikoAl.Data/        ← EF Core (Npgsql) entity + repository + UnitOfWork (EF migrations KAPALI)
  PsikoAl.Services/    ← İş mantığı + Supabase entegrasyon servisleri
  PsikoAl.Api/         ← REST API (frontend'in mock-db sözleşmesinin birebir gerçek hali)
  PsikoAl.Client/      ← Blazor Server admin paneli (Api'ye yalnızca HTTP ile bağlanır)
  *.Tests / *.ContractTests
```

Bağımlılık yönü: `Client → (HTTP) → Api → Services → Data → Common`.

## Geliştirme

```bash
dotnet build src/PsikoAl.slnx
dotnet test  src/PsikoAl.slnx
dotnet run --project src/PsikoAl.Api
```

Supabase anahtarları `src/PsikoAl.Api/appsettings.Local.json` dosyasına yazılır (gitignore'da). Şablon için `docs/SETUP.md`.

## Sözleşme Kaynakları

- `psikoal-app` reposu: `docs/BACKEND_REQUIREMENTS.md`, `docs/ADMIN_PANEL_REQUIREMENTS.md`
- Davranış referansı: `mock-db/handlers/`, tablo referansı: `mock-db/data/*.json`
