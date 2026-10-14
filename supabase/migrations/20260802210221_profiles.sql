-- 0003_profiles: kullanıcı profilleri + auth.users insert trigger'ı + RLS
-- Kaynak sözleşme: native-atomic AuthUserSchema (id, email, firstName, lastName, role,
-- isVerified, avatarUrl, createdAt, phone, city, shareEmail, sharePhone, shareLocation)

create table if not exists public.profiles (
  id uuid primary key references auth.users (id) on delete cascade,
  email text not null,
  first_name text not null default '',
  last_name text not null default '',
  role text not null default 'client' check (role in ('expert', 'client')),
  is_verified boolean not null default false,
  avatar_url text,
  phone text,
  city text,
  share_email boolean not null default true,
  share_phone boolean not null default true,
  share_location boolean not null default true,
  status text not null default 'active' check (status in ('active', 'frozen', 'deleted')),
  created_at timestamptz not null default now(),
  updated_at timestamptz not null default now()
);

create index if not exists idx_profiles_role on public.profiles (role);
create index if not exists idx_profiles_status on public.profiles (status);

create or replace function public.set_updated_at()
returns trigger
language plpgsql
set search_path = public
as $$
begin
  new.updated_at = now();
  return new;
end;
$$;

drop trigger if exists trg_profiles_updated_at on public.profiles;
create trigger trg_profiles_updated_at
  before update on public.profiles
  for each row execute function public.set_updated_at();

-- Yeni auth kullanıcısı → profil satırı (metadata'dan; rol geçersizse client)
create or replace function public.handle_new_user()
returns trigger
language plpgsql
security definer
set search_path = public
as $$
begin
  insert into public.profiles (id, email, first_name, last_name, role)
  values (
    new.id,
    coalesce(new.email, ''),
    coalesce(new.raw_user_meta_data ->> 'first_name', ''),
    coalesce(new.raw_user_meta_data ->> 'last_name', ''),
    case
      when new.raw_user_meta_data ->> 'role' in ('expert', 'client')
        then new.raw_user_meta_data ->> 'role'
      else 'client'
    end
  )
  on conflict (id) do nothing;
  return new;
end;
$$;

drop trigger if exists trg_auth_users_created on auth.users;
create trigger trg_auth_users_created
  after insert on auth.users
  for each row execute function public.handle_new_user();

-- RLS: kullanıcı yalnızca kendi profilini görür/günceller.
-- Backend service_role ile çalışır (RLS bypass); bu politikalar defense-in-depth.
alter table public.profiles enable row level security;

create policy "profiles_owner_select"
  on public.profiles for select
  using (id = auth.uid());

create policy "profiles_owner_update"
  on public.profiles for update
  using (id = auth.uid())
  with check (id = auth.uid());

-- Kolon bazlı koruma: authenticated rol/status/is_verified/email değiştiremez
revoke update on public.profiles from authenticated;
grant update (first_name, last_name, phone, city, avatar_url, share_email, share_phone, share_location)
  on public.profiles to authenticated;
