-- 0006_listings: ilan akışı + admin onay state machine + pg_cron expiry
-- Kaynak: docs/ADMIN_PANEL_REQUIREMENTS.md §1.1, §2.4; docs/BACKEND_REQUIREMENTS.md
--
-- State machine: PENDING_APPROVAL → OPEN → MATCHED → (CLOSED | EXPIRED)
--                              └──────→ REJECTED_BY_ADMIN
-- LISTING_MAX_ACTIVE (PENDING_APPROVAL + OPEN toplamı) DB trigger'ıyla zorlanır.
--
-- NOT: assessment_result_id şimdilik nullable, FK yok — assessment_results tablosu
-- Dilim 7'de gelecek (skill kuralı: backfill sonrası constraint). offer_count de
-- Dilim 5'te offers tablosu gelince canlı sorguya geçecek, şimdilik 0 sabit.

create table if not exists public.listings (
  id uuid primary key default gen_random_uuid(),
  client_id uuid not null references public.profiles (id) on delete cascade,
  title text not null,
  description text not null default '',
  specialization text[] not null default '{}',
  budget_min numeric(10, 2) not null check (budget_min > 0),
  budget_max numeric(10, 2) not null check (budget_max >= budget_min),
  preferred_session_type text not null check (preferred_session_type in ('online', 'yüz_yüze', 'yüz_yüze_online')),
  city text,
  assessment_result_id uuid,
  offer_count int not null default 0,
  status text not null default 'PENDING_APPROVAL'
    check (status in ('PENDING_APPROVAL', 'OPEN', 'MATCHED', 'CLOSED', 'EXPIRED', 'REJECTED_BY_ADMIN')),
  rejection_reason text,
  approved_at timestamptz,
  approved_by uuid references public.admin_users (id),
  expires_at timestamptz,
  created_at timestamptz not null default now(),
  updated_at timestamptz not null default now()
);

create index if not exists idx_listings_client on public.listings (client_id);
create index if not exists idx_listings_status on public.listings (status);
create index if not exists idx_listings_specialization on public.listings using gin (specialization);
create index if not exists idx_listings_expires_at on public.listings (expires_at) where status = 'OPEN';

drop trigger if exists trg_listings_updated_at on public.listings;
create trigger trg_listings_updated_at
  before update on public.listings
  for each row execute function public.set_updated_at();

-- LISTING_MAX_ACTIVE: bir danışanın PENDING_APPROVAL+OPEN ilan toplamı system_settings'i aşamaz.
create or replace function public.enforce_listing_max_active()
returns trigger
language plpgsql
set search_path = public
as $$
declare
  max_active int;
  current_active int;
begin
  select value::int into max_active from public.system_settings where key = 'LISTING_MAX_ACTIVE';

  select count(*) into current_active
  from public.listings
  where client_id = new.client_id and status in ('PENDING_APPROVAL', 'OPEN');

  if current_active >= max_active then
    raise exception 'LISTING_MAX_ACTIVE_EXCEEDED' using errcode = 'P0001';
  end if;

  return new;
end;
$$;

revoke execute on function public.enforce_listing_max_active() from public, anon, authenticated;

drop trigger if exists trg_listings_max_active on public.listings;
create trigger trg_listings_max_active
  before insert on public.listings
  for each row execute function public.enforce_listing_max_active();

-- RLS: OPEN ilanlar herkese (uzman feed'i), sahibi kendi ilanını her durumda görür.
-- Yazma yalnızca backend (service_role) — onay/reddet/kapat state machine'i client'tan izole tutar.
alter table public.listings enable row level security;

create policy "listings_public_read_open_or_owner"
  on public.listings for select
  using (status = 'OPEN' or client_id = auth.uid());

-- pg_cron: OPEN ilanlar süresi dolunca otomatik EXPIRED (saatlik).
create extension if not exists pg_cron;

select cron.schedule(
  'expire_open_listings',
  '0 * * * *',
  $$update public.listings set status = 'EXPIRED' where status = 'OPEN' and expires_at < now()$$
);
