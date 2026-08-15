# Güvenlik Politikası

PsikoAl bir sağlık/psikoloji platformudur ve **özel nitelikli kişisel veri** (KVKK m.6)
işler. Güvenlik açığı bildirimlerini ciddiye alıyoruz.

## Açık bildirimi

- **E-posta:** yazilimuygun@gmail.com — konu satırına `[GÜVENLİK]` yazın

> Bu repo PRIVATE'tır ve GitHub private vulnerability reporting özelliği GitHub Advanced
> Security gerektirdiği için **kapalıdır**. Tek geçerli kanal yukarıdaki e-postadır.

Açığı herkese açık bir yerde paylaşmayın.

## Taahhüdümüz

| Aşama | Süre |
|---|---|
| İlk yanıt | 72 saat içinde |
| Etki değerlendirmesi | 7 gün içinde |
| Düzeltme planı | 14 gün içinde |

## Secret yönetimi

- Lokal geliştirmede anahtarlar **`dotnet user-secrets`** ile verilir;
  şablon: `src/PsikoAl.Api/appsettings.Local.example.json`.
- `service_role` anahtarı ve Postgres parolası **hiçbir koşulda** repoya, log'a veya
  istemciye girmez.
- CI'da `gitleaks` job'u tüm geçmişi tarar. Sızmış bir anahtar bulunursa yapılacak:
  önce **rotate**, sonra geçmiş temizliği. Sadece commit silmek yeterli değildir.
- Supabase legacy JWT rotasyonu `anon` anahtarını da geçersiz kılar — ikisi birden
  yeniden dağıtılmalıdır (mobil istemci dahil).
