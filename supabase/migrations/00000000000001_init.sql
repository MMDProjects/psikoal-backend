-- 0001_init: temel altyapı tabloları (system_settings, audit_logs, admin_users)
-- Kaynak sözleşme: docs/ADMIN_PANEL_REQUIREMENTS.md §8

create extension if not exists "pgcrypto";

create table if not exists public.system_settings (
  key text primary key,
  value text not null,
  description text,
  updated_at timestamptz not null default now(),
  updated_by uuid
);

insert into public.system_settings (key, value, description) values
  ('LISTING_MAX_ACTIVE', '3', 'Danışanın aynı anda açık tutabileceği ilan sayısı'),
  ('LISTING_EXPIRE_DAYS', '30', 'İlanın otomatik EXPIRED olacağı gün sayısı'),
  ('PACKAGE_VALID_DAYS', '180', 'Paket geçerlilik süresi (gün)'),
  ('PACKAGE_MIN_SESSIONS', '3', 'Paketteki minimum seans sayısı'),
  ('PACKAGE_MAX_SESSIONS', '20', 'Paketteki maksimum seans sayısı'),
  ('LISTING_AUTO_APPROVE', 'false', 'İlanlar admin onayı olmadan yayınlansın mı'),
  ('MAINTENANCE_MODE', 'false', 'Bakım modu')
on conflict (key) do nothing;

create table if not exists public.admin_users (
  id uuid primary key default gen_random_uuid(),
  auth_user_id uuid not null unique,
  display_name text not null,
  role text not null check (role in ('super_admin', 'moderator', 'content_editor', 'finance')),
  is_active boolean not null default true,
  created_at timestamptz not null default now()
);

create table if not exists public.audit_logs (
  id bigint generated always as identity primary key,
  admin_user_id uuid,
  actor_type text not null default 'admin' check (actor_type in ('admin', 'system', 'user')),
  action text not null,
  entity_type text not null,
  entity_id text not null,
  old_value jsonb,
  new_value jsonb,
  reason text,
  created_at timestamptz not null default now()
);

create index if not exists idx_audit_logs_entity on public.audit_logs (entity_type, entity_id);
create index if not exists idx_audit_logs_created_at on public.audit_logs (created_at desc);

-- Audit log silinemez/güncellenemez (append-only)
create or replace function public.prevent_audit_mutation()
returns trigger
language plpgsql
as $$
begin
  raise exception 'audit_logs is append-only';
end;
$$;

drop trigger if exists trg_audit_logs_no_update on public.audit_logs;
create trigger trg_audit_logs_no_update
  before update or delete on public.audit_logs
  for each row execute function public.prevent_audit_mutation();

alter table public.system_settings enable row level security;
alter table public.admin_users enable row level security;
alter table public.audit_logs enable row level security;
-- Politikalar bilinçli olarak yok: bu tablolara yalnızca backend (service_role) erişir.
