# Güvenlik Politikası

PsikoAl bir sağlık/psikoloji platformudur ve **özel nitelikli kişisel veri** (KVKK m.6)
işler. Güvenlik açığı bildirimlerini ciddiye alıyoruz.

## Açık bildirimi

- **Tercih edilen kanal:** GitHub Security → *Report a vulnerability*
  (private vulnerability reporting bu repoda açıktır)
- **E-posta:** yazilimuygun@gmail.com — konu satırına `[GÜVENLİK]` yazın

Açığı **herkese açık issue olarak açmayın.**

## Taahhüdümüz

| Aşama | Süre |
|---|---|
| İlk yanıt | 72 saat içinde |
| Etki değerlendirmesi | 7 gün içinde |
| Düzeltme planı | 14 gün içinde |

## Kapsam

Bu repo PUBLIC'tir ve veritabanı şeması, RLS politikaları ile admin panel kodu herkese
açıktır. Bu bilinçli bir tercihtir; güvenlik gizliliğe değil, katmanlara dayanır:

- `service_role` anahtarı ve Postgres parolası hiçbir koşulda repoda, log'da veya
  istemcide bulunmaz. Lokal geliştirmede **`dotnet user-secrets`** kullanılır;
  şablon: `src/PsikoAl.Api/appsettings.Local.example.json`.
- Supabase `anon` anahtarı mobil bundle içinde bulunur — tanım gereği geneldir.
  Veri erişimini **RLS politikaları** korur. Politikalar bu repoda okunabilir olduğu için
  **RLS'i atlatan her bulgu yüksek öncelikli geçerli bir bildirimdir**, lütfen iletin.
- Supabase proje referansı bir kimlik bilgisi değildir; her istek ayrıca kimlik doğrular.
- CI'da her PR'da: `gitleaks` (tüm geçmiş), CodeQL (`csharp`), NetAnalyzers.

Mobil istemci `MMDProjects/psikoal-app` reposundadır.

## Secret sızıntısında yapılacak

Sırayla: **1)** anahtarı rotate et, **2)** sonra geçmişi temizle.
Sadece commit silmek yeterli DEĞİLDİR — public bir repoda içerik zaten kopyalanmış
sayılır. Supabase legacy JWT rotasyonu `anon` anahtarını da geçersiz kılar; ikisi
birden yeniden dağıtılmalıdır (mobil istemci dahil).