create table if not exists public.deneme_kapi (
  id uuid primary key default gen_random_uuid(),
  ad text not null
);

create policy "deneme_kapi_select" on public.deneme_kapi
  for select using (true);

insert into public.deneme_kapi (ad) values ('kapi-testi');
