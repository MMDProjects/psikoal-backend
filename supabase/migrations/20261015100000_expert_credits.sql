-- expert_credits: uzman kredisi (Armut modeli) — uzman teklif GÖNDERİRKEN kredi harcar
-- Kaynak: docs/PAYMENTS.md, docs/ADMIN_PANEL_REQUIREMENTS.md §7.1
--
-- Danışandan para alınmaz. Uzman kredisini yalnız web sitesinden PayTR iFrame API ile
-- satın alır; mobil uygulamada satın alma yolu yoktur (App Store / Play kuralı).
--
-- Bakiye experts.credit_balance'ta TEK satırda tutulur ve yalnız koşullu UPDATE ile
-- değişir (`... set credit_balance = credit_balance + @delta where id = @id and
-- credit_balance + @delta >= 0`). CHECK kısıtı ikinci savunma hattıdır: uygulama bir
-- gün koşulu unutursa bakiye eksiye düşmek yerine işlem patlar.
--
-- credit_transactions bakiyenin defteridir (append-only): her satır bakiyeyi değiştiren
-- tek bir olayı ve sonrasındaki bakiyeyi taşır. UPDATE trigger ile yasak (tek istisna: bağlı
-- teklif/ödeme/admin silinince FK'nin null'a çekilmesi); DELETE yalnız hesabın kendisi
-- (experts satırı) silinirse cascade ile olur.
--
-- Additive/forward-only: mevcut tablo ve veriye dokunmaz; yeni kolon varsayılan 0.
-- Yeni uzmana başlangıç kredisi YOK (ürün kararı, 2026-10-14).

alter table public.experts
  add column if not exists credit_balance int not null default 0;

do $$
begin
  if not exists (
    select 1 from pg_constraint
    where conname = 'experts_credit_balance_non_negative'
      and conrelid = 'public.experts'::regclass
  ) then
    alter table public.experts
      add constraint experts_credit_balance_non_negative check (credit_balance >= 0);
  end if;
end;
$$;

create table if not exists public.credit_packages (
  id uuid primary key default gen_random_uuid(),
  name text not null check (length(btrim(name)) between 1 and 100),
  credits int not null check (credits > 0),
  price_try numeric(10, 2) not null check (price_try > 0),
  is_active boolean not null default true,
  sort_order int not null default 0,
  created_at timestamptz not null default now(),
  updated_at timestamptz not null default now()
);

create index if not exists idx_credit_packages_active_sort on public.credit_packages (is_active, sort_order);

drop trigger if exists trg_credit_packages_updated_at on public.credit_packages;
create trigger trg_credit_packages_updated_at
  before update on public.credit_packages
  for each row execute function public.set_updated_at();

alter table public.credit_packages enable row level security;

-- merchant_oid PayTR şartı: yalnız harf+rakam, en çok 64 karakter.
-- provider_payload hassas alan TAŞIMAZ (hash, kart bilgisi yok): yalnız durum,
-- tutar, ödeme tipi ve başarısızlık nedeni.
create table if not exists public.credit_payments (
  id uuid primary key default gen_random_uuid(),
  expert_id uuid not null references public.experts (id) on delete cascade,
  package_id uuid not null references public.credit_packages (id) on delete restrict,
  merchant_oid text not null unique check (merchant_oid ~ '^[A-Za-z0-9]{1,64}$'),
  amount_try numeric(10, 2) not null check (amount_try > 0),
  credits int not null check (credits > 0),
  status text not null default 'PENDING' check (status in ('PENDING', 'PAID', 'FAILED')),
  provider_payload jsonb,
  created_at timestamptz not null default now(),
  completed_at timestamptz
);

create index if not exists idx_credit_payments_expert on public.credit_payments (expert_id, created_at desc);
create index if not exists idx_credit_payments_status_created on public.credit_payments (status, created_at desc);

alter table public.credit_payments enable row level security;

create table if not exists public.credit_transactions (
  id uuid primary key default gen_random_uuid(),
  expert_id uuid not null references public.experts (id) on delete cascade,
  amount int not null check (amount <> 0),
  balance_after int not null check (balance_after >= 0),
  type text not null check (type in ('PURCHASE', 'OFFER_SPEND', 'ADMIN_ADJUST')),
  offer_id uuid references public.offers (id) on delete set null,
  payment_id uuid references public.credit_payments (id) on delete set null,
  admin_user_id uuid references public.admin_users (id) on delete set null,
  reason text,
  created_at timestamptz not null default now(),
  constraint credit_transactions_adjust_has_reason check (type <> 'ADMIN_ADJUST' or (reason is not null and length(btrim(reason)) > 0))
);

create index if not exists idx_credit_transactions_expert_created on public.credit_transactions (expert_id, created_at desc);

-- Callback idempotency'sinin veritabanı katmanı: aynı ödeme için ikinci PURCHASE satırı
-- yazılamaz. Asıl koruma servisteki koşullu PENDING → PAID geçişidir; bu indeks o
-- koşul bir gün gevşerse krediyi iki kez yüklemeyi imkânsız kılar.
create unique index if not exists uq_credit_transactions_purchase_payment
  on public.credit_transactions (payment_id)
  where type = 'PURCHASE';

create unique index if not exists uq_credit_transactions_offer_spend
  on public.credit_transactions (offer_id)
  where type = 'OFFER_SPEND';

-- Tek izinli UPDATE: bağlı kaydın silinmesiyle FK'nin `on delete set null` ile
-- boşalması (ilan/teklif silindi, admin hesabı kaldırıldı). Tutar, bakiye, tür,
-- gerekçe ve zaman DEĞİŞEMEZ; bir FK'ye yeni bir değer de yazılamaz.
create or replace function public.prevent_credit_transaction_update()
returns trigger
language plpgsql
set search_path = public
as $$
begin
  if new.id = old.id
     and new.expert_id = old.expert_id
     and new.amount = old.amount
     and new.balance_after = old.balance_after
     and new.type = old.type
     and new.reason is not distinct from old.reason
     and new.created_at = old.created_at
     and (new.offer_id is not distinct from old.offer_id or new.offer_id is null)
     and (new.payment_id is not distinct from old.payment_id or new.payment_id is null)
     and (new.admin_user_id is not distinct from old.admin_user_id or new.admin_user_id is null)
  then
    return new;
  end if;

  raise exception 'credit_transactions is append-only';
end;
$$;

revoke execute on function public.prevent_credit_transaction_update() from public, anon, authenticated;

drop trigger if exists trg_credit_transactions_no_update on public.credit_transactions;
create trigger trg_credit_transactions_no_update
  before update on public.credit_transactions
  for each row execute function public.prevent_credit_transaction_update();

alter table public.credit_transactions enable row level security;

-- 20261014100200_revoke_postgrest_table_access ile tutarlı: API `postgres` rolüyle
-- bağlanır, PostgREST kullanılmaz. Varsayılan yetkiler o migration'da kapatıldı; yine
-- de açıkça geri alınır (migration'lar farklı sırayla uygulanmış bir ortamda da kapalı
-- doğsunlar). Politika BİLEREK yok.
revoke all on public.credit_packages from anon, authenticated;
revoke all on public.credit_payments from anon, authenticated;
revoke all on public.credit_transactions from anon, authenticated;

insert into public.system_settings (key, value, description) values
  ('OFFER_CREDIT_COST', '1', 'Uzmanın bir ilana teklif göndermesinin kredi maliyeti (0 = ücretsiz)')
on conflict (key) do nothing;

insert into public.notification_templates (type, title, body, in_app_enabled, push_enabled, email_enabled) values
  ('CREDIT_PURCHASED', 'Krediniz yüklendi', '{credits} kredi hesabınıza yüklendi. Güncel bakiyeniz: {balance} kredi.', true, true, false)
on conflict (type) do nothing;
