-- blogs: içerik dilimi (Dilim 9) — blog yazıları + beğeniler
-- Kaynak: docs/ADMIN_PANEL_REQUIREMENTS.md §3.3, docs/BACKEND_REQUIREMENTS.md "Blog"
-- Sözleşme: native-atomic/src/domains/blog/schemas/blog.schema.ts
--
-- Görünürlük kuralı sorgu anında uygulanır (cron yok):
--   status = 'published'                          → yayında
--   status = 'scheduled' and published_at <= now() → planlanan saat geldi, yayında
--   status = 'draft'                              → yalnız admin görür
-- published_at hem gerçek yayın anını hem plânlı yayın anını tutar; mobil
-- yanıttaki publishedAt doğrudan bu kolondur.
--
-- Kategori bağı: category_id birincil kategori (→ categories, kategori sayfasının
-- "İlgili Bloglar" filtresi categories.blog_tag üzerinden çalışır); tags ise
-- uygulamada #etiket olarak görünen serbest ek etiketlerdir.
--
-- Kapak görseli blog-media bucket'ındaki obje yoludur (URL değil): public URL
-- yanıt anında Supabase:Url'den kurulur, alan adı değişse de kayıt bozulmaz.

create table if not exists public.blogs (
  id uuid primary key default gen_random_uuid(),
  slug text not null unique check (slug ~ '^[a-z0-9]+(-[a-z0-9]+)*$'),
  title text not null,
  excerpt text not null default '',
  content text not null default '',
  cover_image_path text,
  category_id uuid references public.categories (id) on delete set null,
  tags text[] not null default '{}',
  author_name text not null,
  author_title text not null default '',
  reading_minutes int not null default 1 check (reading_minutes > 0),
  status text not null default 'draft' check (status in ('draft', 'published', 'scheduled')),
  published_at timestamptz,
  created_by uuid references public.admin_users (id) on delete set null,
  created_at timestamptz not null default now(),
  updated_at timestamptz not null default now(),
  constraint blogs_published_at_required check (status = 'draft' or published_at is not null),
  constraint blogs_cover_required check (status = 'draft' or cover_image_path is not null)
);

create index if not exists idx_blogs_status_published_at on public.blogs (status, published_at desc);
create index if not exists idx_blogs_category on public.blogs (category_id);
create index if not exists idx_blogs_tags on public.blogs using gin (tags);

drop trigger if exists trg_blogs_updated_at on public.blogs;
create trigger trg_blogs_updated_at
  before update on public.blogs
  for each row execute function public.set_updated_at();

-- API kendi bağlantısıyla (RLS'i aşan rol) okur/yazar. anon/authenticated için
-- policy BİLEREK yok: PostgREST üzerinden taslak ya da plânlı yazı sızmasın.
alter table public.blogs enable row level security;

create table if not exists public.blog_likes (
  blog_id uuid not null references public.blogs (id) on delete cascade,
  user_id uuid not null references public.profiles (id) on delete cascade,
  created_at timestamptz not null default now(),
  primary key (blog_id, user_id)
);

create index if not exists idx_blog_likes_user on public.blog_likes (user_id);

alter table public.blog_likes enable row level security;
