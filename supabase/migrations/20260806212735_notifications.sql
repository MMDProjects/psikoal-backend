-- 0008_notifications: bildirim sablonlari + bildirim gecmisi + push token kaydi
-- Kaynak: docs/ADMIN_PANEL_REQUIREMENTS.md §2.8, §3.6; docs/BACKEND_REQUIREMENTS.md
--
-- Tek kaynak karari: notification_templates bizim DB'mizde (admin panelden CRUD).
-- E-posta gonderirken Brevo'nun kendi template sistemi degil, bu tablodaki
-- html_body ham icerik olarak gonderilir (htmlContent parametresi).

create table if not exists public.notification_templates (
  id uuid primary key default gen_random_uuid(),
  type text not null unique,
  title text not null,
  body text not null,
  html_body text,
  push_enabled boolean not null default true,
  email_enabled boolean not null default false,
  in_app_enabled boolean not null default true,
  is_active boolean not null default true,
  created_at timestamptz not null default now(),
  updated_at timestamptz not null default now()
);

drop trigger if exists trg_notification_templates_updated_at on public.notification_templates;
create trigger trg_notification_templates_updated_at
  before update on public.notification_templates
  for each row execute function public.set_updated_at();

alter table public.notification_templates enable row level security;

insert into public.notification_templates (type, title, body, in_app_enabled, push_enabled, email_enabled) values
  ('OFFER_RECEIVED',   'Yeni teklif aldınız',        '{expertName}, "{listingTitle}" ilanınıza teklif gönderdi.', true, true, false),
  ('OFFER_ACCEPTED',   'Teklifiniz kabul edildi',     '{clientName}, "{listingTitle}" ilanı için teklifinizi kabul etti.', true, true, false),
  ('LISTING_EXPIRING', 'İlanınızın süresi doluyor',   '"{listingTitle}" ilanınızın süresi {daysLeft} gün içinde dolacak.', true, false, false),
  ('LISTING_APPROVED', 'İlanınız onaylandı',          '"{listingTitle}" ilanınız yayına alındı.', true, true, false),
  ('LISTING_REJECTED', 'İlanınız reddedildi',         '"{listingTitle}" ilanınız reddedildi: {reason}', true, true, false),
  ('EXPERT_APPROVED',  'Profiliniz onaylandı',        'Uzman profiliniz onaylandı, artık ilanlara teklif gönderebilirsiniz.', true, true, false),
  ('EXPERT_REJECTED',  'Profiliniz reddedildi',       'Uzman profiliniz reddedildi: {reason}', true, true, false),
  ('REVIEW_APPROVED',  'Yorumunuz onaylandı',         'Bıraktığınız değerlendirme onaylandı ve yayında.', true, false, false),
  ('REVIEW_REJECTED',  'Yorumunuz reddedildi',        'Bıraktığınız değerlendirme reddedildi: {reason}', true, false, false),
  ('SYSTEM',           'Duyuru',                      '{body}', true, false, false)
on conflict (type) do nothing;

create table if not exists public.notifications (
  id uuid primary key default gen_random_uuid(),
  user_id uuid not null references public.profiles (id) on delete cascade,
  type text not null,
  title text not null,
  body text not null,
  data jsonb,
  read boolean not null default false,
  created_at timestamptz not null default now()
);

create index if not exists idx_notifications_user_created on public.notifications (user_id, created_at desc);
create index if not exists idx_notifications_user_unread on public.notifications (user_id) where not read;

alter table public.notifications enable row level security;

create policy "notifications_read_own"
  on public.notifications for select
  using (user_id = auth.uid());

create table if not exists public.push_tokens (
  id uuid primary key default gen_random_uuid(),
  user_id uuid not null references public.profiles (id) on delete cascade,
  token text not null unique,
  platform text not null check (platform in ('ios', 'android')),
  device_id text,
  last_seen_at timestamptz not null default now(),
  created_at timestamptz not null default now()
);

create index if not exists idx_push_tokens_user on public.push_tokens (user_id);

alter table public.push_tokens enable row level security;

create policy "push_tokens_read_own"
  on public.push_tokens for select
  using (user_id = auth.uid());

-- LISTING_EXPIRING: OPEN ilan bitişine 3 gün kalınca bir kez in-app bildirim üretir.
-- Push/email bu adımda kapsam dışı (cron'dan C# tetiklemek ek karmaşıklık gerektirir);
-- kullanıcı in-app + Bildirimler ekranından görür.
alter table public.listings add column if not exists expiry_notified_at timestamptz;

create or replace function public.notify_expiring_listings()
returns void
language plpgsql
set search_path = public
as $$
declare
  v_title text;
  v_body text;
begin
  select title, body into v_title, v_body
  from public.notification_templates
  where type = 'LISTING_EXPIRING' and is_active;

  if v_title is null then
    return;
  end if;

  insert into public.notifications (user_id, type, title, body, data)
  select
    l.client_id,
    'LISTING_EXPIRING',
    v_title,
    replace(replace(v_body, '{listingTitle}', l.title), '{daysLeft}', ceil(extract(epoch from (l.expires_at - now())) / 86400)::text),
    jsonb_build_object('listingId', l.id)
  from public.listings l
  where l.status = 'OPEN'
    and l.expires_at is not null
    and l.expires_at between now() and now() + interval '3 days'
    and l.expiry_notified_at is null;

  update public.listings
  set expiry_notified_at = now()
  where status = 'OPEN'
    and expires_at is not null
    and expires_at between now() and now() + interval '3 days'
    and expiry_notified_at is null;
end;
$$;

revoke execute on function public.notify_expiring_listings() from public, anon, authenticated;

select cron.schedule(
  'notify_expiring_listings',
  '0 * * * *',
  $$select public.notify_expiring_listings()$$
);
