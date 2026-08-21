# Migration'lar

## Adlandırma

`<14 haneli timestamp>_<snake_case>.sql` — Supabase CLI'nin ürettiği biçim.
CI'daki `migration-lint` job'u bu kalıbı zorlar.

> **Dosya adı `*_init.sql` OLAMAZ.** Supabase CLI, adı `init` olan migration'ı
> `supabase start` ve `db reset` sırasında **sessizce atlar**
> ("replace 'init' with a different file name to apply this migration").
> İlk migration bu yüzden `_bootstrap` adını taşıyor; uzak veritabanındaki
> karşılığının adı `init` olsa da eşleşme **sürüm numarasıyla** yapılır, adla değil.

## Uzak tarihçe ile ilişki

Bu klasördeki 9 dosya **konsolide** sürümdür. Prod veritabanının
`supabase_migrations.schema_migrations` tablosunda ise 15 kayıt var: aynı 9 temel
migration + Supabase advisory'lerinden gelen 6 ek düzeltme, MCP üzerinden ayrı ayrı
uygulanmış.

| Yalnız uzakta olan kayıt | İçeriği nereye işlendi |
|---|---|
| `fix_prevent_audit_mutation_search_path` | `20260802203531_bootstrap.sql` |
| `fix_public_bucket_no_list_policy` | `20260802204825_storage_buckets.sql` |
| `lock_down_trigger_functions` | `20260803110920_experts.sql` |
| `fix_expert_ratings_security_invoker` | `20260806192233_categories_reviews.sql` |
| `fix_accept_offer_fk_order` | `20260806202716_offers_matches.sql` |
| `fix_notification_templates_rls` | `20260806212735_notifications.sql` |

Altısının da içeriği ilgili dosyaya **işlenmiştir** — temiz bir ortam bu klasörden
kurulduğunda aynı güvenlik durumuna ulaşır. Bu yüzden ayrı dosya olarak
eklenmediler; `supabase migration list` çıktısında "yalnız uzakta" görünmeleri
beklenen davranıştır ve `db push` onları yok sayar.

## Kurallar

Migration'lar **forward-only ve additive-only** yazılır (expand-contract).
Mobil istemci haftalarca eski şemayla yaşadığı için kolon/tablo silme ayrı bir
sürüme ertelenir. Yeni dosyalarda CI şunları zorunlu tutar:

- Yeni tablo varsa `enable row level security`
- `create policy` öncesinde `drop policy if exists`
- Seed `insert` ifadelerinde `on conflict`
