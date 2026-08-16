# PsikoAl — Proje Brief

> Psikologları ve terapi almak isteyen danışanları buluşturan Türkiye'nin iki taraflı psikoloji pazar yeri.
> **iOS & Android · React Native · Faz 1 MVP · 10 Hafta**

---

## Nedir?

PsikoAl, Türkiye'de psikolog bulmayı Armut.com'daki gibi çalıştıran bir mobil uygulamadır. Danışan ihtiyacını tarif eden bir ilan açar; alanında uzman psikologlar fiyat ve açıklama içeren teklif gönderir; danışan beğendiği teklifi kabul eder, eşleşme gerçekleşir ve terapi başlar.

Platform iki farklı kullanıcı rolüne hizmet eder ve her iki taraf için de ayrı bir deneyim sunar. Psikologlar danışan bulmak için reklam harcamak zorunda kalmaz; danışanlar ise profil ve yorumları inceleyerek güvenle seçim yapar.

---

## Kullanıcı Rolleri

| Rol | Kimdir | Platforma Nasıl Girer | Ne Yapabilir |
|-----|--------|----------------------|--------------|
| **Danışan** | Terapi almak isteyen bireyler | Kayıt ekranından rol seçerek | İlan açar, teklifleri değerlendirir, psikolog seçer, ödeme yapar |
| **Psikolog** | Lisanslı klinik psikologlar | Kayıt sonrası admin onayıyla aktif olur | İlanları görür, teklif sunar, danışan kazanır, kazancını takip eder |

---

## Ana Akış

```
[Danışan]  İlan açar
               ↓
[Psikologlar]  İlanı görür → Teklif gönderir
               ↓
[Danışan]  Teklifleri karşılaştırır → Kabul eder
               ↓
[Platform] Otomatik eşleşme oluşturur
               ↓
[Her ikisi] İletişim bilgileri paylaşılır → Terapi başlar
```

---

## Ekranlar & Özellikler

### 1. Kayıt & Giriş Akışı

**Giriş ekranı**
- E-posta ve şifre ile giriş
- "Şifremi unuttum" akışı — e-posta ile sıfırlama bağlantısı gönderilir
- Giriş başarılıysa kullanıcı rolüne göre ilgili ana ekrana yönlendirilir

**Kayıt ekranı**
- Kullanıcı önce rolünü seçer: **Psikolog** veya **Danışan**
- Seçime göre farklı onboarding akışına yönlendirilir

---

### 2. Psikolog Onboarding (Kayıt Sonrası Profil Kurulumu)

Psikolog kayıt olduktan sonra 5 adımlı bir profil kurulum formunu doldurur. Bu form tamamlanmadan profil yayınlanamaz.

**Adım 1 — Ünvan & Uzmanlık**
- Ünvan: Klinik Psikolog, Psikoterapist, Danışman Psikolog vb.
- Uzmanlık alanları çoklu seçim: Anksiyete, Depresyon, Kaygı Bozukluğu, Panik Atak, OKB, Travma & PTSD, İlişki Sorunları, Aile Terapisi, Çocuk & Ergen, Uyku Bozukluğu, Stres Yönetimi, Yeme Bozukluğu, Bağımlılık, Kişilik Bozuklukları, Motivasyon, İş Stresi, Yas & Kayıp

**Adım 2 — Deneyim**
- Meslekte kaç yıl çalıştığı (sayısal giriş)
- Çalışılan kurumlar veya klinikler (serbest metin)

**Adım 3 — Biyografi**
- Danışanların okuyacağı kısa tanıtım metni (maks. 500 karakter)
- Çalışma yaklaşımı ve yöntem bilgisi

**Adım 4 — Profil Fotoğrafı**
- Galeriden veya kameradan seçim
- Kırpma & döndürme aracı

**Adım 5 — Tamamlandı**
- "Profiliniz incelemeye alındı" ekranı gösterilir
- Admin onayı gelene kadar uygulamaya erişim kısıtlıdır
- Onay bildirimi push notification ile iletilir (genellikle 24–48 saat)

---

### 3. Danışan Onboarding

Danışanlar kayıt olduktan sonra kısa bir form doldurur:
- Ad, soyad, e-posta, şifre
- Opsiyonel: Hangi konuda destek arıyorsunuz? (Anksiyete, Depresyon vb. — tek seçim)
- Bu seçim ilk ilan oluşturmada varsayılan kategori olarak kullanılır

---

### 4. Psikolog Ana Ekranı — Fırsatlar

Psikolog uygulamayı açtığında **OPEN** durumdaki danışan ilanlarını görür. Bu ekran psikoloğun iş akışının merkezidir.

**İlan listesi**
- Her ilan kartında: danışan adı kısaltılmış (gizlilik — "Zeynep Y."), konu başlığı, uzmanlık alanı etiketleri, bütçe aralığı, seans tipi tercihi (online / yüz yüze / her ikisi), kaç teklif geldiği, ilanın açılış tarihi
- İlan kartına tıklanınca ilan detay sayfası açılır

**Filtreler**
- Uzmanlık alanına göre filtre (çoklu seçim)
- Seans tipine göre: Online / Yüz yüze / Fark etmez
- Bütçe aralığına göre: ₺0–500 / ₺500–1500 / ₺1500+

**Boş durum**
- "Henüz uygun ilan yok" mesajı ve filtre sıfırlama butonu

---

### 5. İlan Detayı (Psikolog Görünümü)

Psikolog bir ilan kartına tıkladığında tam ilan içeriğini görür.

**Gösterilen bilgiler**
- Danışan adı kısaltılmış (Zeynep Y.) ve profil avatarı
- İlan başlığı ve açıklama metni
- Uzmanlık alanı etiketleri
- Bütçe aralığı ve tercih edilen seans tipi
- İlan tarihi ve kaç uzmanın teklif gönderdiği
- Danışanın daha önce çözdüğü psikolojik test sonucu (varsa) — psikologun daha iyi değerlendirme yapması için

**Alt aksiyon**
- **Teklif Gönder** butonu — psikoloğun daha önce bu ilana teklif göndermemişse aktif
- Daha önce teklif göndermişse: "Teklifiniz iletildi" durumu ve teklif özeti gösterilir; yeni teklif gönderilemez

---

### 6. Teklif Gönderme (Psikolog)

Psikolog ilan detayından "Teklif Gönder"e tıkladığında teklif formu açılır.

**Form alanları**
- **Fiyat** (₺): Seans başına ücret — sayısal giriş
- **Seans tipi**: Online / Yüz Yüze / Yüz Yüze veya Online — tek seçim chip
- **Açıklama** (opsiyonel): Danışana iletilecek kısa not, maks. 300 karakter (ör. "Hafta içi sabah saatleri müsaitim, CBT odaklı çalışıyorum")

**Gönderim kuralları**
- Psikolog aynı ilana yalnızca 1 teklif gönderebilir
- Teklif gönderildikten sonra fiyat değiştirilemez; teklif geri çekilebilir
- Gönderim başarılıysa "Teklifiniz danışana iletildi" mesajı gösterilir ve Tekliflerim sekmesine eklenir

---

### 7. Tekliflerim (Psikolog)

Psikoloğun gönderdiği tüm tekliflerin listelendiği sekme.

**Teklif durumları**
| Durum | Anlamı |
|-------|--------|
| **Bekliyor** | Danışan henüz yanıt vermedi |
| **Kabul Edildi** | Danışan teklifi kabul etti → eşleşme oluştu |
| **Reddedildi** | Danışan başka bir teklifi seçti |
| **Geri Çekildi** | Psikolog teklifi geri aldı |

**Teklif kartı içeriği**
- İlan başlığı, danışan adı (kısaltılmış), teklif fiyatı, seans tipi, durum rozeti, gönderim tarihi

**Aksiyonlar**
- **Bekliyor** durumundaki teklif → "Teklifi Geri Çek" butonu gösterilir
- **Kabul Edildi** durumundaki teklif → Eşleşme detay sayfasına yönlendirir

---

### 8. Eşleşmelerim (Psikolog)

Psikolog kabul edilen teklifleri bu sekmeden takip eder.

**Sekmeler**
- **Aktif** — devam eden eşleşmeler
- **Geçmiş** — tamamlanan veya sonlandırılan eşleşmeler

**Eşleşme kartı içeriği**
- Danışan tam adı (eşleşme sonrası gizlilik kaldırılır), profil fotoğrafı
- Eşleşme tarihi, ilan konusu, kabul edilen teklif fiyatı
- Eşleşme durumu: Aktif / Tamamlandı / Sonlandırıldı

**Eşleşme detay sayfası**
- Danışan tam adı, e-posta adresi, telefon numarası (platforma kayıtlı)
- Orijinal ilan içeriği ve kabul edilen teklif özeti
- "Eşleşmeyi Sonlandır" — her iki tarafın onayıyla kapanır

---

### 9. Danışan Ana Ekranı — İlanlarım

Danışan uygulamayı açtığında kendi ilanlarını ve eşleşme durumunu görür.

**Aktif eşleşme varsa**
- Ekranın üstünde kalıcı eşleşme kartı: psikolog adı, seans fiyatı, eşleşme tarihi, "Detayı Gör" butonu

**İlan listesi**
- Aktif ilanlar (OPEN durumunda) — teklif sayısı rozetiyle
- Geçmiş ilanlar — eşleşildi / kapatıldı / süresi doldu etiketiyle
- Her ilan kartına tıklanınca ilan detayı ve gelen teklifler görülür

**"İlan Oluştur" butonu**
- Aktif ilan sayısı 3'ün altındaysa aktif; 3 ise devre dışı ve uyarı mesajı gösterilir

---

### 10. İlan Oluşturma (Danışan)

Danışan 3 adımlı form doldurarak ilan açar.

**Adım 1 — Konu & Açıklama**
- **Başlık**: İlanın kısa özeti, 10–100 karakter (ör. "Anksiyete ve panik atak için uzman arıyorum")
- **Açıklama** (opsiyonel): Daha ayrıntılı bilgi, maks. 500 karakter

**Adım 2 — Uzmanlık Alanı**
- Chip çoklu seçim: Anksiyete, Depresyon, Travma, İlişki Sorunları vb.
- En az 1 alan seçilmesi zorunlu

**Adım 3 — Bütçe & Seans Tercihi**
- Minimum bütçe (₺): Seans başı ödeyebileceğiniz en düşük tutar
- Maksimum bütçe (₺): Üst limit; maksimum minimumdan küçük olamaz
- Seans tipi tercihi: Online / Yüz Yüze / Yüz Yüze veya Online — tek seçim

**Opsiyonel: Test sonucu ekleme**
- Daha önce platform üzerinden psikolojik test çözdüyse, sonucu ilana ekleyebilir
- Psikologlar bu sonucu teklif verirken görebilir; daha iyi değerlendirme yapmaları sağlanır

**Gönderim**
- İlan oluşturulduktan sonra otomatik olarak OPEN durumuna geçer
- İlan detay sayfasına yönlendirilir; gelen teklifleri buradan takip eder

---

### 11. İlan Detayı (Danışan Görünümü)

**Gösterilen bilgiler**
- İlan başlığı, açıklaması, uzmanlık alanları, bütçe ve seans tipi
- İlan durumu rozeti: Yayında / Eşleşildi / Kapatıldı / Süresi Doldu
- Kaç uzmanın teklif gönderdiği

**Gelen teklifler listesi**
- Her teklifte: psikolog adı ve avatarı, uzmanlık alanları, puan, teklif fiyatı, seans tipi
- Teklife tıklanınca teklif detayı açılır
- Psikolog avatarına tıklanınca psikolog profil sayfası açılır ("Uzmanı İncele")

**Alt aksiyon**
- İlan OPEN durumdaysa: **İlanı Kapat** butonu

---

### 12. Teklif Detayı (Danışan Görünümü)

**Gösterilen bilgiler**
- Psikolog adı, ünvanı, puanı, profil fotoğrafı
- Teklif fiyatı, seans tipi
- Psikologun yazdığı açıklama notu
- Psikoloğun test sonucu inceleme bilgisi (test eklendiyse)
- Teklif durumu rozeti

**Aksiyonlar (yalnızca PENDING teklif)**
- **Teklifi Kabul Et** — önce onay diyaloğu: "İletişim bilgileriniz psikologla paylaşılacaktır. Devam etmek istiyor musunuz?" → Kabul → eşleşme oluşur
- **Reddet** — teklif REJECTED durumuna geçer, ilana yeni teklif gelmeye devam eder

---

### 13. Eşleşme

Danışan bir teklifi kabul ettiği anda:

1. Sistem otomatik olarak eşleşme kaydını oluşturur
2. O ilandaki diğer tüm PENDING teklifler otomatik REJECTED olur
3. İlan MATCHED durumuna geçer (yeni teklif alamaz, yeniden açılamaz)
4. Psikologun danışanın tam adı, e-postası ve telefon numarasına erişimi açılır
5. Her iki tarafa push bildirim gönderilir: "Eşleşme gerçekleşti!"

**Eşleşme durumları**
| Durum | Anlamı |
|-------|--------|
| **Aktif** | Eşleşme devam ediyor, terapi süreci başlamış |
| **Tamamlandı** | Seans paketi tamamlandı |
| **Sonlandırıldı** | Her iki tarafın onayıyla kapatıldı |

**Sonlandırma kuralı**: Yalnızca her iki taraf da onay verirse eşleşme RELEASED durumuna geçer. Tek taraflı sonlandırma yapılamaz.

---

### 14. Psikolog Profil Sayfası (Herkese Açık)

Hem danışanlar hem de psikologlar bu sayfayı görüntüleyebilir. Danışanlar, teklife tıklayarak veya ilan detayındaki "Uzmanı İncele" bağlantısıyla bu sayfaya erişir.

**Profil içeriği**
- Profil fotoğrafı, tam ad, ünvan
- Doğrulama rozeti (admin onaylı)
- Ortalama puan ve yorum sayısı (yıldız + sayı)
- Uzmanlık alanı etiketleri
- Mesleki deneyim (kaç yıl)
- Biyografi metni
- İstatistik kartları: Toplam danışan sayısı, tamamlanan seans sayısı, ortalama puan
- Yorumlar listesi (en yeni önce)

**Sayfa aksiyonu**
- Danışan için "Bu Uzmana İlan Gönder" CTA butonu gösterilir — tıklayınca ilan oluşturma ekranı açılır, bu uzmanın uzmanlık alanı varsayılan olarak seçili gelir
- Psikologun kendi profil sayfasında düzenleme butonu gösterilir

---

### 15. Ücretsiz Psikolojik Değerlendirme Testi

Platform girişi yapmadan erişilebilen bağımsız bir modüldür. Lead magnet işlevi görür.

**Giriş ekranı**
- Testin adı ve kısa açıklaması
- Tahmini süre (ör. "~5 dakika")
- "Testi Başlat" butonu
- Giriş yapmayan kullanıcılar buraya erişebilir

**Soru akışı**
- Sorular tek tek gösterilir; kullanıcı cevapladıkça bir sonraki soruya geçilir
- Ekranın üstünde ilerleme çubuğu: "Soru 3 / 12" gibi gösterim
- Her soruda 4–5 seçenek (tek seçim)
- "Geri" butonu ile önceki soruya dönülebilir
- Yanıtlar cihazda saklanır; uygulama kapansa bile kaldığı yerden devam eder

**Sonuç ekranı**
- **Skor ve seviye**: Düşük / Orta / Yüksek (renk kodlu: yeşil / sarı / kırmızı)
- **Özet metin**: "Sonuçlarınız anksiyete belirtileri taşıdığını gösteriyor. Profesyonel destek almak faydalı olabilir."
- **Öneri kategorisi**: Hangi uzmanlık alanında psikolog araması yapılması gerektiği
- **CTA**: "Şimdi Uzman Bul" — tıklayınca kayıt/giriş ekranına yönlendirir; giriş yapıldıktan sonra ilan oluşturma formu uzmanlık alanı seçili şekilde açılır
- **Opsiyonel e-posta**: "Sonucu e-posta ile al" — e-posta girilirse 24 saat içinde uzman öneri maili gönderilir; zorunlu değil

**Test sonucunu ilana ekleme**
- Giriş yapan kullanıcı ilan oluştururken önceki test sonucunu ilana ekleyebilir
- Psikologlar bu sonucu görerek daha isabetli teklif sunar

---

### 16. Ödeme Sistemi — Kontör & Paket Satışı

**Kontör cüzdanı**
- Danışanın hesabında bir bakiye bulunur
- Seans onaylandıktan sonra bakiyeden ilgili tutar düşülür, psikoloğa aktarılır
- Kontör bakiyesi uygulama içindeki cüzdan ekranında görüntülenir

**Paket satın alma akışı**
1. Danışan **Paketler** ekranını açar
2. Mevcut paketler kart formatında listelenir: Seans sayısı, toplam fiyat, seans başı ücret, geçerlilik süresi
3. Paket seçilir, "Satın Al" butonuna basılır
4. Iyzico 3D Secure ekranı uygulama içinde açılır (WebView)
5. Ödeme başarılıysa cüzdana kontör eklenir ve "Ödeme tamamlandı" ekranı gösterilir
6. Ödeme başarısızsa hata mesajı ve "Tekrar Dene" butonu gösterilir

**Paket kuralları**
- Minimum 3 seans, maksimum 20 seans tek pakette
- Paket fiyatı, tekil seans ücretinin toplamından mutlaka düşük olmalıdır (indirim zorunluluğu)
- Satın alındıktan itibaren 6 ay geçerlidir
- 6 ay dolunca kullanılmayan seanslar iptal edilir

**Uzman kazanç ekranı**
- Tamamlanan seans sayısı
- Bu aya ait toplam kazanç
- Bekleyen (henüz aktarılmamış) tutar
- Geçmiş işlem listesi: tarih, danışan adı, seans ücreti, platform komisyonu, net tutar

---

### 17. Bildirim Sistemi

**Push bildirimleri** (expo-notifications)

| Olay | Alıcı | Mesaj |
|------|-------|-------|
| Yeni teklif geldi | Danışan | "Anksiyete ilanınıza yeni bir teklif geldi" |
| Teklif güncellendi | Danışan | "Dr. Mert teklifini güncelledi" |
| Teklif kabul edildi | Psikolog | "Zeynep Y. teklifinizi kabul etti — eşleşme gerçekleşti!" |
| Teklif reddedildi | Psikolog | "Teklifiniz değerlendirildi" |
| Eşleşme gerçekleşti | Her ikisi | "Yeni bir eşleşme!" |
| Ödeme başarılı | Danışan | "Ödemeniz alındı, kontörleriniz yüklendi" |
| Seans hatırlatıcısı | Her ikisi | "Yarın saat 14:00'te seans var" |
| İlan süresi doluyor | Danışan | "İlanınız 3 gün içinde kapanacak" |
| Yorum daveti | Danışan | "Seansınızı değerlendirin" (seans tamamlandıktan 2 saat sonra) |

**Bildirim izni kuralı**: İzin, uygulama yüklendiğinde değil, danışanın ilk teklifi aldığında istenir. Bu yaklaşım izin kabul oranını artırır.

**Uygulama içi bildirim merkezi**
- Okunmamış bildirimler üstte, mavi arka planla vurgulanır
- Okunmuş bildirimler gri arka planla listelenir
- Bildirime tıklanınca ilgili sayfaya yönlendirilir (ilan, teklif, eşleşme)

---

### 18. Yorum & Değerlendirme Sistemi

**Yorum yazma koşulu**
- Yalnızca tamamlanmış seans kaydı olan danışan yorum yazabilir
- Seans tamamlandıktan 2 saat sonra otomatik push bildirimi gider: "Psikologunuzu değerlendirin"

**Yorum formu**
- 1–5 yıldız puanı (zorunlu)
- Yazılı yorum (opsiyonel, maks. 500 karakter)
- Anonimleştirme seçeneği: "Adımı gizle" işaretlenirse yorum "Anonim Danışan" olarak gösterilir
- Bir danışan aynı uzmana yalnızca 1 yorum yapabilir

**Psikolog yanıt hakkı**
- Her yoruma bir kez yanıt verilebilir (maks. 300 karakter)
- Yanıt, yorum kartının altında "Uzman Yanıtı" etiketiyle gösterilir

**Yorum moderasyonu**
- Kullanıcılar yorumu şikayet edebilir
- Şikayet edilen yorumlar admin inceleme kuyruğuna alınır

---

### 19. Ayarlar & Profil Sekmesi

**Her iki rol için ortak**
- Profil bilgilerini düzenle (ad, e-posta, telefon, profil fotoğrafı)
- Şifre değiştir
- Bildirim tercihleri: hangi olayda hangi kanaldan bildirim alınacağı (push / e-posta / SMS)
- Gizlilik politikası ve kullanım koşulları
- Hesabı sil
- Çıkış yap

**Psikolog için ek seçenekler**
- Profil düzenleme: ünvan, biyografi, uzmanlık alanları, fotoğraf
- Kazanç ve ödeme bilgileri
- IBAN / banka hesabı güncelleme

---

## Durum Makineleri (İş Kuralları)

### İlan Durumları

```
         OPEN
          │
     ┌────┴────────┐
     │             │
  Teklif kabul   Danışan kapatır
     │             │
  MATCHED       CLOSED
     │
  30 gün geçer
     │
  EXPIRED
```

- **OPEN**: Danışan ilan açtı, psikologlar teklif gönderebilir
- **MATCHED**: Bir teklif kabul edildi, diğer teklifler otomatik reddedildi
- **CLOSED**: Danışan ilanı manuel kapattı
- **EXPIRED**: 30 gün sonra otomatik kapandı
- MATCHED veya EXPIRED ilan hiçbir koşulda yeniden açılamaz

### Teklif Durumları

```
PENDING ──→ ACCEPTED  (danışan kabul etti → eşleşme oluştu)
        ──→ REJECTED  (danışan başka teklifi seçti)
        ──→ WITHDRAWN (psikolog geri çekti)
```

### Eşleşme Durumları

```
ACTIVE ──→ COMPLETED  (seans paketi tamamlandı)
       ──→ RELEASED   (her iki taraf onayladı)
```

---

## Sayfa Haritası

```
(Giriş Yapılmamış)
├── /assessment          ← Ücretsiz psikolojik test (auth gerekmez)
├── /assessment/result   ← Test sonucu
├── /login               ← Giriş
└── /register            ← Kayıt (rol seçimi)
    ├── /onboarding/expert   ← Psikolog profil kurulumu
    └── /onboarding/client   ← Danışan kısa kurulum

(Giriş Yapılmış — Ortak)
├── /expert/[id]         ← Psikolog profil sayfası (herkese açık görünüm)
├── /payment/packages    ← Paket satın alma
└── /payment/checkout    ← Iyzico ödeme (WebView)

(Tab Bar — Psikolog)
├── / (Fırsatlar)        ← Açık ilanlar feed'i
├── /offers (Tekliflerim) ← Gönderilen teklifler
├── /matches (Eşleşmelerim) ← Aktif ve geçmiş eşleşmeler
└── /profile (Ayarlar)

(Tab Bar — Danışan)
├── / (İlanlarım)        ← Kendi ilanları + eşleşme durumu
├── /offers (İlanlarım)  ← İlan oluştur + aktif/geçmiş ilanlar
├── /notifications (Bildirimler)
└── /profile (Ayarlar)

(Detay Sayfaları)
├── /listing/new         ← İlan oluşturma formu
├── /listing/[id]        ← İlan detayı + gelen teklifler
├── /offer/new           ← Teklif gönderme formu
├── /offer/[id]          ← Teklif detayı + aksiyonlar
├── /match/[id]          ← Eşleşme detayı (psikolog only)
└── /client/[id]         ← Danışan profili (psikolog only)
```

---

## Geliştirme Takvimi

| Hafta | Aşama | İçerik |
|-------|-------|--------|
| H1–H2 | **Ekran Tasarımları** ⬤ | Tüm ekranların Figma tasarımı, marka kimliği, renk & tipografi |
| H3–H4 | Temel Altyapı & Auth | Proje kurulumu, giriş/kayıt, onboarding akışları, UI bileşen kütüphanesi |
| H3–H4 | Uzman & Danışan Profilleri | Profil sayfaları, admin onay akışı, profil düzenleme |
| H4–H5 | İlan Sistemi | İlan oluşturma, ilan listesi, ilan detayı, durum yönetimi |
| H5–H6 | Teklif & Eşleşme · Değerlendirme Testi | Teklif akışı, otomatik eşleşme, ücretsiz test modülü |
| H6–H7 | Ödeme Sistemi | Kontör cüzdanı, paket satışı, Iyzico 3D Secure entegrasyonu |
| H7–H8 | Bildirimler & Yorum Sistemi | Push bildirimler, yorum yazma, moderasyon |
| H8–H9 | Test & Son Düzeltmeler | Kullanıcı akışı testleri, hata yönetimi, App Store hazırlığı |
| H10 | **Lansman** | App Store & Google Play yayını, beta psikolog grubu |

---

## Faz 1 Kapsam Özeti

**Dahil**
- iOS & Android mobil uygulama
- Psikolog profili & admin onay akışı
- İlan, teklif ve eşleşme sistemi (tam durum makineleriyle)
- Kontör & paket satışı (Iyzico 3D Secure)
- Ücretsiz psikolojik değerlendirme testi
- Push bildirimler
- Yorum & değerlendirme sistemi

**Faz 2'ye Bırakılan**
- Web uygulaması
- Admin yönetim paneli
- Danışanın doğrudan platform kaydı (Faz 1'de psikolog davet eder)
- Gelişmiş eşleşme algoritması (yapay zeka destekli öneri)
- Kurumsal / klinik panel
- Video görüşme entegrasyonu

---

## Teknoloji

| Alan | Seçim | Neden |
|------|-------|-------|
| Mobil | React Native + Expo SDK 56 | iOS & Android tek kod tabanı |
| Navigasyon | Expo Router v6 | Dosya bazlı, tip güvenli route sistemi |
| Veri yönetimi | TanStack Query + Zustand | Server state ve UI state net ayrımı |
| Stil | NativeWind (Tailwind CSS) | Hızlı geliştirme, tutarlı tasarım sistemi |
| Ödeme | Iyzico 3D Secure | Türkiye'nin en yaygın ödeme altyapısı |
| Depolama | MMKV | AsyncStorage'dan 10x hızlı, şifreli |
| Bildirimler | expo-notifications + FCM | iOS & Android çapraz platform push |

---

*Tekin Labs × PsikoAl — Temmuz 2026 · v2.0*
