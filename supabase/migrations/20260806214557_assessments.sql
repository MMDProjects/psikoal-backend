-- 0009_assessments: psikolojik test modülü (auth gerektirmez)
-- Kaynak: docs/BACKEND_REQUIREMENTS.md, docs/ADMIN_PANEL_REQUIREMENTS.md §3
--
-- Skorlama kuralı Postgres/C# sınırı: C# toplam puanı hesaplar, hangi bant
-- (level/summary/suggestions) uygulanacağını assessment_score_rules'tan okur —
-- admin panelden düzenlenebilir, hardcoded eşik yok.

create table if not exists public.assessments (
  id uuid primary key default gen_random_uuid(),
  title text not null,
  description text not null default '',
  category text not null,
  estimated_minutes int not null default 3,
  sort_order int not null default 0,
  is_active boolean not null default true,
  created_at timestamptz not null default now(),
  updated_at timestamptz not null default now()
);

drop trigger if exists trg_assessments_updated_at on public.assessments;
create trigger trg_assessments_updated_at
  before update on public.assessments
  for each row execute function public.set_updated_at();

alter table public.assessments enable row level security;
create policy "assessments_public_read_active"
  on public.assessments for select
  using (is_active);

create table if not exists public.assessment_questions (
  id uuid primary key default gen_random_uuid(),
  assessment_id uuid not null references public.assessments (id) on delete cascade,
  text text not null,
  type text not null check (type in ('single_choice', 'multiple_choice', 'scale')),
  sort_order int not null default 0,
  options jsonb not null default '[]'
);

create index if not exists idx_assessment_questions_assessment on public.assessment_questions (assessment_id, sort_order);

alter table public.assessment_questions enable row level security;
create policy "assessment_questions_public_read"
  on public.assessment_questions for select
  using (exists (
    select 1 from public.assessments a where a.id = assessment_questions.assessment_id and a.is_active
  ));

create table if not exists public.assessment_score_rules (
  id uuid primary key default gen_random_uuid(),
  assessment_id uuid not null references public.assessments (id) on delete cascade,
  min_score int not null,
  max_score int not null,
  level text not null check (level in ('low', 'moderate', 'high')),
  summary text not null,
  suggestions text[] not null default '{}',
  sort_order int not null default 0
);

create index if not exists idx_assessment_score_rules_assessment on public.assessment_score_rules (assessment_id, min_score);

alter table public.assessment_score_rules enable row level security;

create table if not exists public.assessment_results (
  id uuid primary key default gen_random_uuid(),
  assessment_id uuid not null references public.assessments (id) on delete cascade,
  user_id uuid references public.profiles (id) on delete set null,
  score int not null,
  level text not null check (level in ('low', 'moderate', 'high')),
  summary text not null,
  suggestions text[] not null default '{}',
  email text,
  created_at timestamptz not null default now()
);

create index if not exists idx_assessment_results_user on public.assessment_results (user_id, created_at desc);

alter table public.assessment_results enable row level security;
create policy "assessment_results_read_own"
  on public.assessment_results for select
  using (user_id = auth.uid());

-- listings.assessment_result_id artık gerçek FK — 0006'da hiç dolu değildi
-- (assessment_results tablosu bu ana kadar yoktu), backfill gerekmiyor.
alter table public.listings
  add constraint fk_listings_assessment_result foreign key (assessment_result_id) references public.assessment_results (id) on delete set null;

-- Seed: mock-db/data/assessment.json ile birebir (3 test, GAD-7/PHQ-9/PSS tabanlı).
do $$
declare
  v_anxiety_id uuid := gen_random_uuid();
  v_depression_id uuid := gen_random_uuid();
  v_stress_id uuid := gen_random_uuid();
begin
  insert into public.assessments (id, title, description, category, estimated_minutes, sort_order) values
    (v_anxiety_id, 'Kaygı Değerlendirme', 'Son iki hafta içinde kendinizi ne sıklıkla aşağıdaki durumlarda buldunuz? Bu test tanı koymaz, genel bir değerlendirme sunar.', 'anxiety', 3, 10),
    (v_depression_id, 'Depresyon Tarama', 'Son iki hafta içinde kendinizi ne sıklıkla aşağıdaki durumlarda buldunuz? PHQ-9 tabanlı bu test tanı koymaz.', 'depression', 4, 20),
    (v_stress_id, 'Stres Değerlendirme', 'Son bir ay içindeki deneyimlerinizi değerlendirin. Bu test algılanan stres düzeyinizi ölçer, tanı koymaz.', 'stress', 3, 30);

  insert into public.assessment_questions (assessment_id, text, type, sort_order, options) values
    (v_anxiety_id, 'Gergin, endişeli veya sinirli hissettim.', 'single_choice', 1, '[{"id":"q1a","text":"Hiç","value":0},{"id":"q1b","text":"Birkaç gün","value":1},{"id":"q1c","text":"Günlerin yarısından fazla","value":2},{"id":"q1d","text":"Neredeyse her gün","value":3}]'),
    (v_anxiety_id, 'Endişelerimi durduramadım veya kontrol edemedim.', 'single_choice', 2, '[{"id":"q2a","text":"Hiç","value":0},{"id":"q2b","text":"Birkaç gün","value":1},{"id":"q2c","text":"Günlerin yarısından fazla","value":2},{"id":"q2d","text":"Neredeyse her gün","value":3}]'),
    (v_anxiety_id, 'Farklı konularda aşırı endişelendim.', 'single_choice', 3, '[{"id":"q3a","text":"Hiç","value":0},{"id":"q3b","text":"Birkaç gün","value":1},{"id":"q3c","text":"Günlerin yarısından fazla","value":2},{"id":"q3d","text":"Neredeyse her gün","value":3}]'),
    (v_anxiety_id, 'Rahatlayamadım, içim sıkıştı.', 'single_choice', 4, '[{"id":"q4a","text":"Hiç","value":0},{"id":"q4b","text":"Birkaç gün","value":1},{"id":"q4c","text":"Günlerin yarısından fazla","value":2},{"id":"q4d","text":"Neredeyse her gün","value":3}]'),
    (v_anxiety_id, 'Yerimde duramayacak kadar huzursuz veya tedirgin hissettim.', 'single_choice', 5, '[{"id":"q5a","text":"Hiç","value":0},{"id":"q5b","text":"Birkaç gün","value":1},{"id":"q5c","text":"Günlerin yarısından fazla","value":2},{"id":"q5d","text":"Neredeyse her gün","value":3}]'),
    (v_anxiety_id, 'Kolayca sinirlenip tahammülsüz hale geldim.', 'single_choice', 6, '[{"id":"q6a","text":"Hiç","value":0},{"id":"q6b","text":"Birkaç gün","value":1},{"id":"q6c","text":"Günlerin yarısından fazla","value":2},{"id":"q6d","text":"Neredeyse her gün","value":3}]'),
    (v_anxiety_id, 'Korku duygusuyla bir şeylerin çok kötü gidebileceğini hissettim.', 'single_choice', 7, '[{"id":"q7a","text":"Hiç","value":0},{"id":"q7b","text":"Birkaç gün","value":1},{"id":"q7c","text":"Günlerin yarısından fazla","value":2},{"id":"q7d","text":"Neredeyse her gün","value":3}]');

  insert into public.assessment_questions (assessment_id, text, type, sort_order, options) values
    (v_depression_id, 'İşlere karşı ilgi veya zevk duymama.', 'single_choice', 1, '[{"id":"d1a","text":"Hiç","value":0},{"id":"d1b","text":"Birkaç gün","value":1},{"id":"d1c","text":"Günlerin yarısından fazla","value":2},{"id":"d1d","text":"Neredeyse her gün","value":3}]'),
    (v_depression_id, 'Kendimi kötü, çökmüş ya da umutsuz hissettim.', 'single_choice', 2, '[{"id":"d2a","text":"Hiç","value":0},{"id":"d2b","text":"Birkaç gün","value":1},{"id":"d2c","text":"Günlerin yarısından fazla","value":2},{"id":"d2d","text":"Neredeyse her gün","value":3}]'),
    (v_depression_id, 'Uykuya dalmakta zorlandım ya da çok fazla uyudum.', 'single_choice', 3, '[{"id":"d3a","text":"Hiç","value":0},{"id":"d3b","text":"Birkaç gün","value":1},{"id":"d3c","text":"Günlerin yarısından fazla","value":2},{"id":"d3d","text":"Neredeyse her gün","value":3}]'),
    (v_depression_id, 'Yorgun hissettim ya da enerjim yoktu.', 'single_choice', 4, '[{"id":"d4a","text":"Hiç","value":0},{"id":"d4b","text":"Birkaç gün","value":1},{"id":"d4c","text":"Günlerin yarısından fazla","value":2},{"id":"d4d","text":"Neredeyse her gün","value":3}]'),
    (v_depression_id, 'İştahsızlık ya da aşırı yeme.', 'single_choice', 5, '[{"id":"d5a","text":"Hiç","value":0},{"id":"d5b","text":"Birkaç gün","value":1},{"id":"d5c","text":"Günlerin yarısından fazla","value":2},{"id":"d5d","text":"Neredeyse her gün","value":3}]'),
    (v_depression_id, 'Kendimi başarısız hissettim ya da kendimi ve ailemi hayal kırıklığına uğrattığımı düşündüm.', 'single_choice', 6, '[{"id":"d6a","text":"Hiç","value":0},{"id":"d6b","text":"Birkaç gün","value":1},{"id":"d6c","text":"Günlerin yarısından fazla","value":2},{"id":"d6d","text":"Neredeyse her gün","value":3}]'),
    (v_depression_id, 'Konsantre olmakta zorlandım; örneğin gazete okurken veya televizyon izlerken.', 'single_choice', 7, '[{"id":"d7a","text":"Hiç","value":0},{"id":"d7b","text":"Birkaç gün","value":1},{"id":"d7c","text":"Günlerin yarısından fazla","value":2},{"id":"d7d","text":"Neredeyse her gün","value":3}]');

  insert into public.assessment_questions (assessment_id, text, type, sort_order, options) values
    (v_stress_id, 'Beklenmedik bir şeyin olmasıyla ne sıklıkla bunaldınız?', 'single_choice', 1, '[{"id":"s1a","text":"Hiç","value":0},{"id":"s1b","text":"Nadiren","value":1},{"id":"s1c","text":"Bazen","value":2},{"id":"s1d","text":"Sıklıkla","value":3}]'),
    (v_stress_id, 'Hayatınızdaki önemli şeyleri kontrol edemediğinizi ne sıklıkla hissettiniz?', 'single_choice', 2, '[{"id":"s2a","text":"Hiç","value":0},{"id":"s2b","text":"Nadiren","value":1},{"id":"s2c","text":"Bazen","value":2},{"id":"s2d","text":"Sıklıkla","value":3}]'),
    (v_stress_id, 'Gergin ve stresli hissettiniz mi?', 'single_choice', 3, '[{"id":"s3a","text":"Hiç","value":0},{"id":"s3b","text":"Nadiren","value":1},{"id":"s3c","text":"Bazen","value":2},{"id":"s3d","text":"Sıklıkla","value":3}]'),
    (v_stress_id, 'Kişisel sorunlarınızla başa çıkamama konusunda ne sıklıkla kendinizi yetersiz hissettiniz?', 'single_choice', 4, '[{"id":"s4a","text":"Hiç","value":0},{"id":"s4b","text":"Nadiren","value":1},{"id":"s4c","text":"Bazen","value":2},{"id":"s4d","text":"Sıklıkla","value":3}]'),
    (v_stress_id, 'İşlerin istediğiniz gibi gitmediğini ne sıklıkla hissettiniz?', 'single_choice', 5, '[{"id":"s5a","text":"Hiç","value":0},{"id":"s5b","text":"Nadiren","value":1},{"id":"s5c","text":"Bazen","value":2},{"id":"s5d","text":"Sıklıkla","value":3}]'),
    (v_stress_id, 'Üstesinden gelemeyeceğiniz şeyler biriktiğini ne sıklıkla hissettiniz?', 'single_choice', 6, '[{"id":"s6a","text":"Hiç","value":0},{"id":"s6b","text":"Nadiren","value":1},{"id":"s6c","text":"Bazen","value":2},{"id":"s6d","text":"Sıklıkla","value":3}]'),
    (v_stress_id, 'Sinirlilik ve öfkenizi kontrol edebildiniz mi?', 'single_choice', 7, '[{"id":"s7a","text":"Her zaman","value":0},{"id":"s7b","text":"Çoğunlukla","value":1},{"id":"s7c","text":"Bazen","value":2},{"id":"s7d","text":"Nadiren","value":3}]');

  insert into public.assessment_score_rules (assessment_id, min_score, max_score, level, summary, suggestions, sort_order)
  select id, 0, 4, 'low',
    'Değerlendirmenize göre düzeyiniz minimal görünmektedir. Günlük yaşam kalitenizi korumaya devam etmeniz önerilir.',
    array['Düzenli fiziksel aktivite yapın (haftada en az 3 gün, 30 dakika)', 'Uyku düzeninize dikkat edin', 'Mindfulness veya nefes egzersizleri deneyin'],
    1
  from public.assessments where id in (v_anxiety_id, v_depression_id, v_stress_id);

  insert into public.assessment_score_rules (assessment_id, min_score, max_score, level, summary, suggestions, sort_order)
  select id, 5, 9, 'moderate',
    'Değerlendirmenize göre orta düzeyde belirtiler gözlemlenmektedir. Bir uzmanla görüşmeniz faydalı olabilir.',
    array['Tetikleyicilerinizi bir günlük tutarak takip edin', 'Derin nefes ve gevşeme tekniklerini öğrenin', 'Kafein ve alkol tüketimini azaltın', 'Bir psikolog ile en az birkaç seans değerlendirme yapın'],
    2
  from public.assessments where id in (v_anxiety_id, v_depression_id, v_stress_id);

  insert into public.assessment_score_rules (assessment_id, min_score, max_score, level, summary, suggestions, sort_order)
  select id, 10, 21, 'high',
    'Değerlendirmenize göre belirgin belirtiler tespit edilmektedir. Bir uzmandan destek almanız önerilir.',
    array['En kısa sürede bir klinik psikolog veya psikiyatrist ile görüşün', 'Bilişsel Davranışçı Terapi (BDT) için uzman desteği alın', 'Günlük rutininizde stres azaltıcı aktivitelere yer açın', 'Yakın çevrenizle duygularınızı paylaşmaktan çekinmeyin'],
    3
  from public.assessments where id in (v_anxiety_id, v_depression_id, v_stress_id);
end $$;
