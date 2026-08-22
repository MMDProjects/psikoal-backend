-- SYSTEM bildirim şablonunun başlığı sabit 'Duyuru' idi ve admin'in gönderdiği
-- AdminSendNotificationDto.Title hiçbir yerde kullanılmıyordu: panelden "İlanınız
-- yayında" başlığıyla gönderilen bildirim mobilde "Duyuru" olarak görünüyordu.
-- Belirti sessizdi — HTTP 200 dönüyor, bildirim gerçekten üretiliyor, yalnız
-- başlık kayboluyordu.
--
-- Başlık artık gövde gibi değişkenden gelir. Koşul bilinçli: admin panelinden
-- (PATCH /admin/notification-templates/SYSTEM) özelleştirilmiş bir başlık varsa
-- bu migration ona dokunmaz.
update public.notification_templates
set title = '{title}'
where type = 'SYSTEM'
  and title = 'Duyuru';
