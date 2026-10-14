-- 20261014100000_profile_consents: kayıtta KVKK açık rızasının kanıtı
-- Kaynak: ortak sözleşme §1 (2026-10-14). POST /auth/register termsAccepted +
-- healthDataConsent zorunlu; backend rızanın zamanını ve metin sürümünü yazar.
--
-- Additive-only: kolonlar nullable. Bu migration'dan önce açılmış hesaplarda rıza
-- kaydı YOKTUR (null) — geriye dönük uydurma tarih yazılmaz; o hesaplardan rıza
-- uygulama içinde ayrıca alınmalıdır.

alter table public.profiles add column if not exists terms_accepted_at timestamptz;
alter table public.profiles add column if not exists health_data_consent_at timestamptz;
alter table public.profiles add column if not exists consent_version text;

comment on column public.profiles.terms_accepted_at is
  'Kullanım Koşulları + Gizlilik Politikası kabul zamanı (KVKK aydınlatma).';
comment on column public.profiles.health_data_consent_at is
  'Özel nitelikli sağlık verisinin işlenmesine açık rıza zamanı (KVKK m.6).';
comment on column public.profiles.consent_version is
  'Rızanın verildiği metin sürümü (API config Legal:ConsentVersion).';
