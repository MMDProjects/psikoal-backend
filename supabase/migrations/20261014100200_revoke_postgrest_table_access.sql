-- 20261014100200_revoke_postgrest_table_access: public şemasını PostgREST'e kapatır
--
-- Supabase, public şemasındaki her tabloya anon ve authenticated rollerine varsayılan
-- olarak TÜM yetkileri verir; erişimi yalnız RLS politikaları sınırlıyordu. Örneğin
-- listings_public_read_open_or_owner, experts_public_read_approved ve
-- reviews_public_read_approved politikaları anon anahtarla (uygulamanın içinde gömülü,
-- gizli olmayan anahtar) /rest/v1/listings çağrısına açık ilan metinlerini — sağlık
-- bilgisi içeren serbest metin — ve danışan kimliklerini döndürüyordu.
--
-- Doğrulama (2026-10-14): ne mobil uygulama (native-atomic: supabase-js yok, yalnız C#
-- API'ye HTTP) ne de API (Npgsql ile `postgres` rolüyle bağlanır; GoTrue/Storage'a
-- yalnız HTTP) PostgREST kullanıyor. Bu yüzden authenticated de kapatılıyor.
--
-- Etkilenmeyenler:
--  * storage.objects politikaları (avatars/documents klasör kuralları) — storage şeması.
--  * API (`postgres` tablo sahibi) ve service_role.
--  * RLS politikaları yerinde kalır: yetki geri verilirse ikinci savunma hattıdır.
--  * Fonksiyon EXECUTE yetkileri önceki migration'larda tek tek kapatıldı.
--
-- Additive/forward-only: yalnız yetki daraltır, veri ve şema değişmez. Geri almak
-- gerekirse ilgili tabloya hedefli `grant select ... to authenticated` yazılır.

revoke all on all tables in schema public from anon, authenticated;
revoke all on all sequences in schema public from anon, authenticated;

-- Bundan sonra açılacak tablolar da varsayılan olarak kapalı doğsun.
alter default privileges in schema public revoke all on tables from anon, authenticated;
alter default privileges in schema public revoke all on sequences from anon, authenticated;
