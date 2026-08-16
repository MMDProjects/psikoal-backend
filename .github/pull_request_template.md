## Özet

<!-- Ne değişti, tek paragraf. -->

## User Story / İş Kodu

## Değişiklik Tipi

- [ ] feat — yeni özellik
- [ ] fix — hata düzeltmesi
- [ ] refactor — davranış değişmiyor
- [ ] infra / ci
- [ ] docs

## Test Edildi mi?

- [ ] `dotnet build src/PsikoAl.slnx -warnaserror`
- [ ] `dotnet test src/PsikoAl.slnx`
- [ ] Lokal Supabase stack'te `supabase db reset` temiz geçti (migration içeren PR'da)

## Migration Bölümü (migration içeren PR'larda ZORUNLU)

- [ ] Migration **forward-only ve additive-only** (kolon/tablo silme yok — expand-contract)
- [ ] Yeni tabloda `enable row level security` açık
- [ ] `create policy` öncesi `drop policy if exists` var (tekrar koşulabilir)
- [ ] Seed `insert` ifadelerinde `on conflict` var (idempotent)
- [ ] Prod'a uygulanma sırası düşünüldü; mobil istemci eski şemayla çalışmaya devam ediyor

## Kontrol Listesi

- [ ] DTO değişikliği varsa `psikoal-app` Zod şeması ile eşleşiyor (sözleşme drift'i yok)
- [ ] Yeni `ErrorKey` eklendiyse TR mesaj karşılığı tanımlı
- [ ] Yeni servis/akış için `PsikoAl.Services.Tests` altında test var
- [ ] Secret/anahtar koda girmedi (`dotnet user-secrets` kullanıldı)

## Ek Notlar
