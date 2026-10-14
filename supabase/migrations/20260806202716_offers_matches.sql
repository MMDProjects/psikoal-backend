-- 0007_offers_matches: teklif akışı + eşleşme + accept_offer() atomik fonksiyonu
-- Kaynak: docs/BACKEND_REQUIREMENTS.md, docs/ADMIN_PANEL_REQUIREMENTS.md §2.6
--
-- Teklif kabulü çok tablolu atomik bir işlemdir (teklif kabul + diğer PENDING'ler
-- REJECTED + listing→MATCHED + match insert) — bu yüzden Postgres fonksiyonu olarak
-- tek transaction'da yazılır (Postgres/C# sınır kararı, bkz. ROADMAP.md).
--
-- Reviews finalize: match_id artık matches tablosuna FK + NOT NULL (0005'te ertelenmişti).
-- Şu ana kadar hiçbir gerçek match oluşmadığı için mevcut (varsa) test yorumları silinir.

create table if not exists public.offers (
  id uuid primary key default gen_random_uuid(),
  listing_id uuid not null references public.listings (id) on delete cascade,
  expert_id uuid not null references public.experts (id) on delete cascade,
  title text,
  price numeric(10, 2) not null check (price > 0),
  description text not null default '',
  session_type text not null check (session_type in ('online', 'yüz_yüze', 'yüz_yüze_online')),
  status text not null default 'PENDING'
    check (status in ('PENDING', 'ACCEPTED', 'REJECTED', 'WITHDRAWN')),
  match_id uuid,
  created_at timestamptz not null default now(),
  updated_at timestamptz not null default now()
);

create unique index if not exists uq_offers_listing_expert on public.offers (listing_id, expert_id);
create index if not exists idx_offers_expert_status on public.offers (expert_id, status);
create index if not exists idx_offers_listing_status on public.offers (listing_id, status);

drop trigger if exists trg_offers_updated_at on public.offers;
create trigger trg_offers_updated_at
  before update on public.offers
  for each row execute function public.set_updated_at();

create table if not exists public.matches (
  id uuid primary key default gen_random_uuid(),
  listing_id uuid not null references public.listings (id) on delete cascade,
  accepted_offer_id uuid not null unique references public.offers (id) on delete cascade,
  client_id uuid not null references public.profiles (id) on delete cascade,
  expert_id uuid not null references public.experts (id) on delete cascade,
  status text not null default 'ACTIVE' check (status in ('ACTIVE', 'COMPLETED', 'RELEASED')),
  client_released_at timestamptz,
  expert_released_at timestamptz,
  released_by_admin boolean not null default false,
  release_reason text,
  created_at timestamptz not null default now(),
  updated_at timestamptz not null default now()
);

alter table public.offers
  add constraint fk_offers_match foreign key (match_id) references public.matches (id) on delete set null;

create index if not exists idx_matches_client on public.matches (client_id);
create index if not exists idx_matches_expert on public.matches (expert_id);
create index if not exists idx_matches_status on public.matches (status);

drop trigger if exists trg_matches_updated_at on public.matches;
create trigger trg_matches_updated_at
  before update on public.matches
  for each row execute function public.set_updated_at();

alter table public.offers enable row level security;
alter table public.matches enable row level security;

create policy "offers_read_participant"
  on public.offers for select
  using (
    expert_id = auth.uid()
    or exists (select 1 from public.listings l where l.id = offers.listing_id and l.client_id = auth.uid())
  );

create policy "matches_read_participant"
  on public.matches for select
  using (client_id = auth.uid() or expert_id = auth.uid());

-- accept_offer: teklif kabulü tek transaction'da — rakip PENDING teklifler REJECTED,
-- ilan MATCHED, match oluşturulur. Satır kilidi (for update) yarış durumunu önler.
create or replace function public.accept_offer(p_offer_id uuid, p_actor_client_id uuid)
returns table (match_id uuid)
language plpgsql
set search_path = public
as $$
declare
  v_offer public.offers%rowtype;
  v_listing public.listings%rowtype;
  v_match_id uuid;
begin
  select * into v_offer from public.offers where id = p_offer_id for update;
  if not found then
    raise exception 'OFFER_NOT_FOUND' using errcode = 'P0001';
  end if;

  select * into v_listing from public.listings where id = v_offer.listing_id for update;
  if not found then
    raise exception 'LISTING_NOT_FOUND' using errcode = 'P0001';
  end if;

  if v_listing.client_id <> p_actor_client_id then
    raise exception 'LISTING_NOT_FOUND' using errcode = 'P0001';
  end if;

  if v_offer.status <> 'PENDING' then
    raise exception 'OFFER_NOT_PENDING' using errcode = 'P0001';
  end if;

  if v_listing.status <> 'OPEN' then
    raise exception 'LISTING_NOT_OPEN' using errcode = 'P0001';
  end if;

  v_match_id := gen_random_uuid();

  update public.offers set status = 'REJECTED'
    where listing_id = v_offer.listing_id and id <> p_offer_id and status = 'PENDING';

  update public.listings set status = 'MATCHED'
    where id = v_offer.listing_id;

  -- matches satırı, offers.match_id'nin işaret edeceği FK hedefinden önce var olmalı.
  insert into public.matches (id, listing_id, accepted_offer_id, client_id, expert_id, status)
  values (v_match_id, v_offer.listing_id, p_offer_id, v_listing.client_id, v_offer.expert_id, 'ACTIVE');

  update public.offers set status = 'ACCEPTED', match_id = v_match_id
    where id = p_offer_id;

  return query select v_match_id;
end;
$$;

revoke execute on function public.accept_offer(uuid, uuid) from public, anon, authenticated;

-- listings.offer_count artık gerçek offers tablosundan sayılabilir; C# tarafı
-- teklif oluşturulunca atomik increment yapar (bkz. OfferService).

-- Reviews finalize: match_id NOT NULL + FK. Henüz hiçbir gerçek match yoktu,
-- bu yüzden backfill = eldeki (varsa test) kayıtları temizlemek.
delete from public.reviews where match_id is null;
drop index if exists uq_reviews_client_expert;
alter table public.reviews alter column match_id set not null;
alter table public.reviews
  add constraint fk_reviews_match foreign key (match_id) references public.matches (id) on delete cascade;
create unique index if not exists uq_reviews_client_match on public.reviews (client_id, match_id);
