-- 0002_storage_buckets: avatars (public-read), blog-media (public-read), documents (private)
-- Kaynak: docs/ADMIN_PANEL_REQUIREMENTS.md, docs/BACKEND_REQUIREMENTS.md (avatarUrl/coverImage düz URL olarak tüketiliyor)

insert into storage.buckets (id, name, public, file_size_limit, allowed_mime_types)
values
  ('avatars', 'avatars', true, 5242880, array['image/png','image/jpeg','image/webp']),
  ('blog-media', 'blog-media', true, 10485760, array['image/png','image/jpeg','image/webp']),
  ('documents', 'documents', false, 10485760, array['application/pdf','image/png','image/jpeg'])
on conflict (id) do nothing;

-- avatars: public bucket zaten URL üzerinden okunabiliyor — ayrı SELECT policy'si
-- LIST API'sini açıp tüm dosya adlarının enumerate edilmesine izin verir, bilinçli olarak eklenmez.
-- Kullanıcı sadece kendi klasörüne (auth.uid()) yazabilir/silebilir.
create policy "avatars_owner_write"
  on storage.objects for insert
  with check (bucket_id = 'avatars' and (storage.foldername(name))[1] = auth.uid()::text);

create policy "avatars_owner_update"
  on storage.objects for update
  using (bucket_id = 'avatars' and (storage.foldername(name))[1] = auth.uid()::text);

create policy "avatars_owner_delete"
  on storage.objects for delete
  using (bucket_id = 'avatars' and (storage.foldername(name))[1] = auth.uid()::text);

-- blog-media: aynı gerekçeyle SELECT policy'si yok. Yazma yalnızca backend (service_role,
-- RLS'i zaten bypass eder) — admin panel yönetir, kullanıcı bazlı policy gerekmiyor.

-- documents: yalnızca sahibi kendi klasörüne erişebilir (admin erişimi backend service_role ile ayrıca sağlanır)
create policy "documents_owner_read"
  on storage.objects for select
  using (bucket_id = 'documents' and (storage.foldername(name))[1] = auth.uid()::text);

create policy "documents_owner_write"
  on storage.objects for insert
  with check (bucket_id = 'documents' and (storage.foldername(name))[1] = auth.uid()::text);

create policy "documents_owner_update"
  on storage.objects for update
  using (bucket_id = 'documents' and (storage.foldername(name))[1] = auth.uid()::text);

create policy "documents_owner_delete"
  on storage.objects for delete
  using (bucket_id = 'documents' and (storage.foldername(name))[1] = auth.uid()::text);
