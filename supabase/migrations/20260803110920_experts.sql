-- 0004_experts: uzman profilleri + versiyonlu admin onayı
-- Kaynak sözleşme: native-atomic ExpertSchema + docs/ADMIN_PANEL_REQUIREMENTS.md §2/§8
-- firstName/lastName/avatarUrl/isVerified profiles'tan gelir (tek kaynak, join ile).

create table if not exists public.experts (
  id uuid primary key references public.profiles (id) on delete cascade,
  title text not null,
  specializations text[] not null default '{}',
  experience_years int not null default 0 check (experience_years between 0 and 50),
  bio text not null default '',
  education text,
  cv_url text,
  certificates text[] not null default '{}',
  personal_website text,
  status text not null default 'pending' check (status in ('pending', 'approved', 'rejected')),
  rejection_reason text,
  approved_at timestamptz,
  approved_by uuid references public.admin_users (id),
  -- Onaylı profil kritik alan güncellediğinde canlı satır değişmez; değişiklik burada
  -- bekler, admin onaylayınca satıra uygulanır (versiyonlu onay).
  pending_revision jsonb,
  created_at timestamptz not null default now(),
  updated_at timestamptz not null default now()
);

create index if not exists idx_experts_status on public.experts (status);
create index if not exists idx_experts_specializations on public.experts using gin (specializations);

drop trigger if exists trg_experts_updated_at on public.experts;
create trigger trg_experts_updated_at
  before update on public.experts
  for each row execute function public.set_updated_at();

-- RLS: onaylı uzmanlar herkese görünür (public profil), uzman kendisini her durumda görür.
-- Yazma yalnızca backend (service_role) üzerinden — owner update policy'si bilinçli yok:
-- tüm alanlar admin onayına tabi, doğrudan client yazımı versiyonlu onayı atlatırdı.
alter table public.experts enable row level security;

create policy "experts_public_read_approved"
  on public.experts for select
  using (status = 'approved' or id = auth.uid());

-- Trigger fonksiyonları yalnızca trigger context'inde çalışmalı; PostgREST RPC
-- üzerinden anon/authenticated çağrısına kapatılır (advisory: 0028/0029).
revoke execute on function public.handle_new_user() from public, anon, authenticated;
revoke execute on function public.set_updated_at() from public, anon, authenticated;
revoke execute on function public.prevent_audit_mutation() from public, anon, authenticated;
