-- KAPI TESTİ — bu migration bilerek bozuk yazıldı, merge EDİLMEYECEK.
-- Beklenen: migration-lint job'u üç ihlali de yakalar.
--   1) create table var ama enable row level security yok
--   2) create policy var ama öncesinde drop policy if exists yok
--   3) insert var ama on conflict yok

create table if not exists public.deneme_kapi (
  id uuid primary key default gen_random_uuid(),
  ad text not null
);

create policy "deneme_kapi_select" on public.deneme_kapi
  for select using (true);

insert into public.deneme_kapi (ad) values ('kapi-testi');
