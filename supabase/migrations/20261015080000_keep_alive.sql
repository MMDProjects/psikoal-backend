-- API'yi uyanık tutma (keep-alive).
--
-- Render ücretsiz planı, 15 dakika boyunca istek gelmeyen servisi uyutur; ilk istek ~1 dk
-- bekler. Ping Render'ın İÇİNDEN atılamaz (uyuyan serviste kod çalışmaz), bu yüzden
-- veritabanındaki pg_cron her 10 dakikada bir pg_net ile API'ye istek atar.
-- /health veritabanını da yokladığı için Supabase ücretsiz planının "bir hafta
-- etkinlik yoksa projeyi duraklat" kuralı da tetiklenmez.
--
-- Admin panelinden yönetilir (Ayarlar): KEEP_ALIVE_ENABLED açar/kapatır,
-- KEEP_ALIVE_URL ping adresidir (yalnız https). Varsayılan KAPALI: lokal ve CI stack'i
-- dışarıya istek atmaz.
--
-- Render ücretsiz planı ayda 750 saati TÜM ücretsiz servisler arasında paylaştırır;
-- 7/24 uyanık tek servis ~744 saat harcar. Yalnız API'ye ping atılmalı, admin paneline değil.

create extension if not exists pg_net;
create extension if not exists pg_cron;

insert into public.system_settings (key, value, description) values
  ('KEEP_ALIVE_ENABLED', 'false', 'API her 10 dakikada bir pinglensin mi (Render ücretsiz planında uykuyu önler)'),
  ('KEEP_ALIVE_URL', '', 'Ping adresi, ör. https://psikoal-api.onrender.com/health (yalnız https)')
on conflict (key) do nothing;

create or replace function public.keep_alive_ping()
returns void
language plpgsql
security definer
set search_path = ''
as $$
declare
  v_enabled text;
  v_url text;
begin
  select value into v_enabled from public.system_settings where key = 'KEEP_ALIVE_ENABLED';
  select value into v_url from public.system_settings where key = 'KEEP_ALIVE_URL';

  if lower(coalesce(v_enabled, 'false')) <> 'true' or coalesce(v_url, '') !~ '^https://' then
    return;
  end if;

  perform net.http_get(url := v_url, timeout_milliseconds := 60000);
end;
$$;

revoke execute on function public.keep_alive_ping() from public, anon, authenticated;

-- cron.schedule aynı adla çağrılınca mevcut işi günceller (idempotent).
select cron.schedule('keep_alive_ping', '*/10 * * * *', 'select public.keep_alive_ping()');
