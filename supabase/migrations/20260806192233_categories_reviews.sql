-- 0005_categories_reviews: kategoriler (tek kaynak: uzmanlık sözlüğü) + yorum moderasyonu
-- Kaynak: docs/ADMIN_PANEL_REQUIREMENTS.md §1.3, §2.6 (17 kayıt = mock-db/data/categories.json)
--
-- NOT: Yorum, gerçekte tamamlanmış bir eşleşmeye (match) bağlı olmalı, ama matches tablosu
-- Dilim 5'te gelecek. match_id şimdilik nullable; Dilim 5'te NOT NULL + FK eklenip
-- backfill edilecek (skill kuralı: drop/NOT NULL öncesi backfill).

create table if not exists public.categories (
  id uuid primary key default gen_random_uuid(),
  slug text not null unique,
  name text not null unique,
  icon text not null,
  summary text not null,
  description text not null,
  blog_tag text not null,
  assessment_category text,
  is_active boolean not null default true,
  sort_order int not null default 0,
  created_at timestamptz not null default now(),
  updated_at timestamptz not null default now()
);

drop trigger if exists trg_categories_updated_at on public.categories;
create trigger trg_categories_updated_at
  before update on public.categories
  for each row execute function public.set_updated_at();

insert into public.categories (slug, name, icon, summary, description, blog_tag, assessment_category, sort_order) values
  ('anksiyete', 'Anksiyete ve Kaygı', 'HeartPulse', 'Kaygı ve huzursuzlukla baş etmenin yolu', 'Anksiyete, sürekli kaygı, gerginlik ve tedirginlik hissiyle kendini gösteren yaygın bir ruhsal deneyimdir. Doğru teknik ve profesyonel destekle yönetilebilir, günlük yaşamınız üzerindeki etkisi azaltılabilir.', 'anksiyete', 'anxiety', 10),
  ('depresyon', 'Depresyon', 'CloudRain', 'Düşük ruh hali için profesyonel destek', 'Depresyon yalnızca üzgünlük değil; enerji kaybı, isteksizlik ve odaklanma güçlüğü gibi belirtilerle seyreden karmaşık bir tablodur. Erken destek, iyileşme sürecini önemli ölçüde hızlandırır.', 'depresyon', 'depression', 20),
  ('iliski-problemleri', 'İlişki Problemleri', 'Heart', 'İlişkilerinizde sağlıklı iletişim için destek', 'Partnerinizle, ailenizle ya da yakın çevrenizle yaşanan iletişim ve güven sorunları ilişki terapisiyle ele alınabilir. Sağlıklı sınırlar ve iletişim becerileri kazandırılır.', 'iliski', null, 30),
  ('travma-tssb', 'Travma ve TSSB', 'Shield', 'Geçmiş travmaların etkisini azaltmak için destek', 'Travmatik deneyimlerin bıraktığı izler zamanla iyileşmeyebilir. Travma odaklı terapi yaklaşımlarıyla bu etkileri azaltmak ve günlük yaşama sağlıklı biçimde dönmek mümkündür.', 'travma', null, 40),
  ('bagimlilik', 'Bağımlılık', 'Wine', 'Bağımlılıkla mücadelede profesyonel destek', 'Madde, davranış ya da dijital bağımlılıklarla mücadele etmek yalnız başına zor olabilir. Uzman desteğiyle sağlıklı ve sürdürülebilir bir iyileşme süreci mümkündür.', 'bagimlilik', null, 50),
  ('aile-terapisi', 'Aile Terapisi', 'Users', 'Aile içi iletişimi güçlendirmek için terapi', 'Aile içi çatışmaları sağlıklı biçimde çözmek ve iletişimi güçlendirmek için aile terapisi, tüm üyelerin sürece dahil olduğu bütüncül bir yaklaşım sunar.', 'terapi', null, 60),
  ('cift-terapisi', 'Çift Terapisi', 'Heart', 'Çiftler arası iletişimi güçlendirmek için destek', 'Çift terapisi, partnerler arasındaki iletişim ve güven sorunlarını sağlıklı sınırlar ve etkili iletişim becerileriyle ele alır.', 'ciftterapisi', null, 70),
  ('cocuk-ergen', 'Çocuk ve Ergen', 'User', 'Çocuk ve ergenler için özel destek', 'Çocukların ve ergenlik dönemindeki gençlerin duygusal, davranışsal ve gelişimsel ihtiyaçları yaş grubuna özel yaklaşımlarla ele alınmalıdır. Bu alanda deneyimli uzmanlar destek verir.', 'cocukergen', null, 80),
  ('kisilik-bozukluklari', 'Kişilik Bozuklukları', 'Fingerprint', 'Kişilik özellikleriyle sağlıklı bir yaşam', 'Kişilik özelliklerinizin günlük yaşamınızı ve ilişkilerinizi zorlaştırdığını düşünüyorsanız uzman bir psikologla bu süreci uzun vadeli bir terapi çalışmasıyla ele alabilirsiniz.', 'kisilik', null, 90),
  ('yeme-bozuklugu', 'Yeme Bozuklukları', 'Utensils', 'Yeme alışkanlıklarında sağlıklı dengeyi bulmak', 'Yeme alışkanlıklarıyla ilgili zorlanmalar hem bedensel hem ruhsal sağlığı etkiler. Bu alanda deneyimli uzmanlar bütüncül bir iyileşme süreci sunar.', 'yeme', null, 100),
  ('uyku-sorunlari', 'Uyku Sorunları', 'Moon', 'Dinlendirmeyen uykuyla baş etmenin yolları', 'Uykuya dalmakta güçlük, sık uyanma ya da dinlendirmeyen uyku ruh sağlığını doğrudan etkiler. Uyku hijyeni ve gerektiğinde profesyonel destekle bu döngü kırılabilir.', 'uyku', null, 110),
  ('ofke-yonetimi', 'Öfke Yönetimi', 'Zap', 'Öfke duygusuyla sağlıklı baş etme', 'Kontrol edilmesi güç öfke tepkileri günlük yaşamı ve ilişkileri zorlaştırabilir. Uzman desteğiyle öfkeyi tanımak ve sağlıklı biçimde ifade etmek mümkündür.', 'ofke', null, 120),
  ('ozguven-benlik', 'Özgüven ve Benlik', 'Sparkles', 'Kendine güveni yeniden inşa etmek', 'Düşük özgüven ve benlik algısı günlük kararları ve ilişkileri etkileyebilir. Terapiyle sağlıklı bir benlik algısı ve özgüven yeniden kazanılabilir.', 'ozguven', null, 130),
  ('kayip-yas', 'Kayıp ve Yas', 'Umbrella', 'Kayıp sonrası yas sürecinde yanınızdayız', 'Bir kaybın ardından yaşanan yas süreci her kişide farklı seyreder. Uzman desteği, bu süreci sağlıklı biçimde deneyimlemek için önemli bir kaynaktır.', 'yaskayip', null, 140),
  ('kariyer-is-stresi', 'Kariyer ve İş Stresi', 'Briefcase', 'Tükenmişlikle baş etmek için destek', 'Tükenmişlik, iş-yaşam dengesizliği ve yoğun çalışma temposu uzun vadede ruh sağlığını ciddi biçimde etkiler. Uzman desteğiyle sağlıklı sınırlar kurmak mümkündür.', 'isstresi', 'stress', 150),
  ('panik-atak', 'Panik Atak', 'Zap', 'Ani gelen yoğun korku atakları için destek', 'Çarpıntı, nefes darlığı ve yoğun korku ile gelen panik ataklar tedavi edilebilir bir durumdur. Uzman desteğiyle atakların sıklığı ve şiddeti azaltılabilir.', 'panikatak', null, 160),
  ('okb', 'OKB', 'RefreshCw', 'Takıntılı düşüncelerle mücadelede uzman desteği', 'Obsesif Kompulsif Bozukluk, tekrarlayan takıntılı düşünceler ve bunları hafifletmeye yönelik zorlayıcı davranışlarla seyreder. Bilişsel Davranışçı Terapi bu alanda en etkili yöntemlerden biridir.', 'okb', null, 170)
on conflict (slug) do nothing;

alter table public.categories enable row level security;

create policy "categories_public_read_active"
  on public.categories for select
  using (is_active);

-- Reviews: danışan yorumu bırakır (pending), yalnızca approved olanlar rating'e girer ve herkese görünür.
create table if not exists public.reviews (
  id uuid primary key default gen_random_uuid(),
  expert_id uuid not null references public.experts (id) on delete cascade,
  client_id uuid not null references public.profiles (id) on delete cascade,
  match_id uuid, -- Dilim 5'te matches tablosuna FK + NOT NULL olacak
  rating int not null check (rating between 1 and 5),
  comment text not null,
  session_type text check (session_type in ('online', 'yüz_yüze', 'yüz_yüze_online')),
  status text not null default 'pending' check (status in ('pending', 'approved', 'rejected')),
  rejection_reason text,
  moderated_at timestamptz,
  moderated_by uuid references public.admin_users (id),
  created_at timestamptz not null default now(),
  updated_at timestamptz not null default now()
);

create index if not exists idx_reviews_expert_status on public.reviews (expert_id, status);
create unique index if not exists uq_reviews_client_expert on public.reviews (client_id, expert_id);

drop trigger if exists trg_reviews_updated_at on public.reviews;
create trigger trg_reviews_updated_at
  before update on public.reviews
  for each row execute function public.set_updated_at();

alter table public.reviews enable row level security;

create policy "reviews_public_read_approved"
  on public.reviews for select
  using (status = 'approved' or client_id = auth.uid());

-- Rating agregasyonu Postgres'te (Postgres/C# sınırı kararı): yalnızca approved yorumlar,
-- backend her expert sorgusunda bu view'dan okur — admin bir yorumu onaylayıp/reddedince
-- ayrı bir "recalc" adımına gerek kalmaz, view her zaman güncel.
create or replace view public.expert_ratings
with (security_invoker = true)
as
select
  expert_id,
  round(avg(rating)::numeric, 1) as rating,
  count(*)::int as review_count
from public.reviews
where status = 'approved'
group by expert_id;
