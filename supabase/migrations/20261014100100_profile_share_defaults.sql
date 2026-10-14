-- 20261014100100_profile_share_defaults: iletişim paylaşımı varsayılan olarak KAPALI
-- KVKK m.4 (ölçülülük) + "privacy by default": yeni hesabın e-posta, telefon ve konumu,
-- kullanıcı açıkça açmadıkça eşleştiği tarafa gösterilmez.
--
-- Yalnız varsayılan değişir; mevcut satırlara DOKUNULMAZ — kullanıcının bugüne kadar
-- verdiği tercih (ya da sessiz kabul) bu migration ile ezilmez.

alter table public.profiles alter column share_email set default false;
alter table public.profiles alter column share_phone set default false;
alter table public.profiles alter column share_location set default false;
