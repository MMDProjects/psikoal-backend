// Demo verisini GERÇEK uç noktalar üzerinden basar ve her çağrıyı raporlar.
//
// KULLANIM:  node scripts/seed-demo.mjs
// Ön koşul:  scripts/e2e-stack.sh ile stack ayakta (API 5089, Supabase 54321/54322).
//
// Neden SQL değil de HTTP: amaç veriyi oluşturmak DEĞİL, veriyi oluşturan yolun
// çalıştığını kanıtlamak. SQL ile INSERT etmek validator'ları, yetki kontrollerini,
// state machine'i ve audit log'u tamamen atlar — yani test edilmek istenen her şeyi.
//
// Zincir:
//   admin kategori ekler → uzman kaydolur + profil açar → admin onaylar/doğrular
//   → danışan ilan açar → admin onaylar → uzman teklif verir → danışan kabul eder
//   → iki taraf da bırakır → danışan yorum yazar → admin yorumu onaylar
//   → admin bildirim gönderir

const API = process.env.E2E_API_URL ?? 'http://localhost:5089'

const ADMIN = { email: 'e2e-admin@psikoal.test', password: 'E2eAdminPass123!' }

// DEMO danışanı, E2E danışanından AYRI olmak zorunda.
// e2e-client@psikoal.test hesabı .NET E2E testlerinin paylaşılan fixture'ıdır ve
// SystemUsersChainTests LISTING_MAX_ACTIVE'i 1'e çekip o hesapla ölçüm yapıyor —
// demo ilanları oraya basılırsa o test "danışanın zaten 3 aktif ilanı var" diye
// kırmızı olur. Belirti alakasız görünür, sebep tamamen saklanır.
const CLIENT = {
  email: 'demo-client@psikoal.test',
  password: 'DemoClient123!',
  firstName: 'Ayşe',
  lastName: 'Yılmaz',
}

// Kanonik kategori listesi migration'ın İÇİNDE seed'li:
//   supabase/migrations/20260806192233_categories_reviews.sql:28
// Buradaki liste onun aynası; ayrışırlarsa seed 422 verir ve sebep görünür.
// DİKKAT: native-atomic/mock-db/data/categories.json BAYAT — 7 slug'ı farklı
// (travma-ptsd / iliski-sorunlari / uyku-bozuklugu / stres-yonetimi /
// motivasyon / is-stresi / yas-kayip). Oradan seed etmek çift kayıt üretir.
const CANONICAL_CATEGORIES = [
  'anksiyete', 'depresyon', 'iliski-problemleri', 'travma-tssb', 'bagimlilik',
  'aile-terapisi', 'cift-terapisi', 'cocuk-ergen', 'kisilik-bozukluklari',
  'yeme-bozuklugu', 'uyku-sorunlari', 'ofke-yonetimi', 'ozguven-benlik',
  'kayip-yas', 'kariyer-is-stresi', 'panik-atak', 'okb',
]

// specializations ve specialization alanları kategori SLUG'ını değil ADINI tutar
// (ExpertService/ListingService → CategoryService.AllActiveNamesExistAsync).
// Mobil de aynısını yapıyor: OnboardingStepSpecs.tsx onToggleSpec(category.name).
const CAT = {
  anksiyete: 'Anksiyete ve Kaygı',
  depresyon: 'Depresyon',
  iliski: 'İlişki Problemleri',
  cift: 'Çift Terapisi',
  aile: 'Aile Terapisi',
  panik: 'Panik Atak',
  okb: 'OKB',
  kayip: 'Kayıp ve Yas',
  ozguven: 'Özgüven ve Benlik',
  kariyer: 'Kariyer ve İş Stresi',
}

const EXPERTS = [
  {
    email: 'demo-expert-1@psikoal.test',
    password: 'DemoExpert123!',
    firstName: 'Elif',
    lastName: 'Kaya',
    profile: {
      title: 'Klinik Psikolog',
      specializations: [CAT.anksiyete, CAT.panik, CAT.okb],
      experienceYears: 9,
      bio: 'Bilişsel davranışçı terapi ağırlıklı çalışıyorum. Anksiyete bozuklukları, panik atak ve obsesif kompulsif bozukluk alanlarında dokuz yıldır yetişkinlerle seans yürütüyorum. Seanslarımda somut ödevler ve ölçülebilir hedefler kullanırım.',
      avatarUrl: 'https://images.psikoal.test/avatars/elif-kaya.jpg',
      education: 'İstanbul Üniversitesi — Klinik Psikoloji Yüksek Lisans',
      cvUrl: 'https://cdn.psikoal.test/cv/elif-kaya.pdf',
      certificates: ['BDT Uygulayıcı Sertifikası', 'EMDR Level 1'],
      personalWebsite: 'https://elifkaya.psikoal.test',
    },
  },
  {
    email: 'demo-expert-2@psikoal.test',
    password: 'DemoExpert123!',
    firstName: 'Mehmet',
    lastName: 'Demir',
    profile: {
      title: 'Uzman Psikolog',
      specializations: [CAT.depresyon, CAT.kayip, CAT.ozguven],
      experienceYears: 14,
      bio: 'Şema terapi ve kabul kararlılık terapisi yaklaşımlarını birlikte kullanıyorum. Depresyon, yas süreçleri ve benlik değeri sorunları üzerine on dört yıllık klinik deneyimim var. Uzun soluklu terapi süreçlerinde çalışmayı tercih ediyorum.',
      avatarUrl: 'https://images.psikoal.test/avatars/mehmet-demir.jpg',
      education: 'Hacettepe Üniversitesi — Psikoloji Doktora',
      cvUrl: 'https://cdn.psikoal.test/cv/mehmet-demir.pdf',
      certificates: ['Şema Terapi Sertifikası'],
      personalWebsite: null,
    },
  },
  {
    email: 'demo-expert-3@psikoal.test',
    password: 'DemoExpert123!',
    firstName: 'Zeynep',
    lastName: 'Aksoy',
    profile: {
      title: 'Aile ve Çift Terapisti',
      specializations: [CAT.cift, CAT.aile, CAT.iliski],
      experienceYears: 6,
      bio: 'Sistemik aile terapisi eğitimi aldım ve çiftlerle iletişim çatışmaları üzerine çalışıyorum. İlişki krizleri, ebeveynlik uyuşmazlıkları ve ayrılık süreçlerinde altı yıldır danışanlara eşlik ediyorum. Seanslarımı çoğunlukla çift olarak yürütürüm.',
      avatarUrl: null,
      education: 'Bahçeşehir Üniversitesi — Aile Danışmanlığı Yüksek Lisans',
      cvUrl: null,
      certificates: ['Sistemik Aile Terapisi Sertifikası', 'Gottman Level 2'],
      personalWebsite: null,
    },
  },
]

const LISTINGS = [
  {
    title: 'Panik atak ataklarım için haftalık online destek arıyorum',
    description:
      'Yaklaşık altı aydır beklenmedik anlarda çarpıntı ve nefes darlığı yaşıyorum. Toplu taşımada başlıyor, sonrasında gün boyu tedirginlik sürüyor. BDT ile çalışan, akşam saatlerinde online görüşebilecek bir uzman arıyorum.',
    specialization: [CAT.panik, CAT.anksiyete],
    budgetMin: 800,
    budgetMax: 1400,
    preferredSessionType: 'online',
    city: 'İstanbul',
    approve: true,
  },
  {
    title: 'İş yerindeki tükenmişlik ve motivasyon kaybı için destek',
    description:
      'Son bir yıldır işe gitmek istemiyorum, sabahları kalkmakta zorlanıyorum. Kariyer değişikliği düşünüyorum ama karar veremiyorum. Yüz yüze veya online fark etmez, hafta içi gündüz uygunum.',
    specialization: [CAT.depresyon, CAT.kariyer],
    budgetMin: 1000,
    budgetMax: 1800,
    preferredSessionType: 'yüz_yüze_online',
    city: 'Ankara',
    approve: true,
  },
  {
    title: 'Eşimle iletişim sorunlarımız için çift terapisi',
    description:
      'Üç yıllık evliyiz, son aylarda her konuşma tartışmaya dönüyor. İkimiz de sürece açığız. Hafta sonu yüz yüze görüşebileceğimiz bir çift terapisti arıyoruz.',
    specialization: [CAT.cift, CAT.iliski],
    budgetMin: 1500,
    budgetMax: 2500,
    preferredSessionType: 'yüz_yüze',
    city: 'İzmir',
    approve: true,
  },
  {
    title: 'Yakın kaybı sonrası yas sürecinde destek almak istiyorum',
    description:
      'Babamı üç ay önce kaybettim. Günlük hayata dönmekte zorlanıyorum ve çevremdekilerle paylaşmak istemiyorum. Online görüşmeyi tercih ederim.',
    specialization: [CAT.kayip],
    budgetMin: 700,
    budgetMax: 1200,
    preferredSessionType: 'online',
    city: 'İstanbul',
    approve: false, // bilinçli: mobilde PENDING_APPROVAL durumu görünsün
  },
]

// ---------------------------------------------------------------- HTTP katmanı

const calls = []
let failed = 0

/**
 * Tek bir HTTP çağrısı yapar, sonucu hem ekrana hem rapora yazar.
 * `soft` verilirse başarısızlık sayaca işlenmez (zaten var olan kayıt gibi
 * beklenen durumlar için).
 */
async function call(method, path, { token, body, soft = false, expected = [], label = '' } = {}) {
  const headers = { Accept: 'application/json' }
  if (token) headers.Authorization = `Bearer ${token}`
  if (body !== undefined) headers['Content-Type'] = 'application/json'

  let response
  let text = ''
  try {
    // auth uçlarında IP başına dakikada 10 istek var ve sayaç SÜREÇLER ARASI
    // yaşar — script'i arka arkaya iki kez koşturmak 429 üretir. Beklemek
    // doğru davranış: 429'u hata olarak raporlamak sözleşme hatası sanılır.
    for (let attempt = 0; ; attempt += 1) {
      response = await fetch(`${API}${path}`, {
        method,
        headers,
        body: body === undefined ? undefined : JSON.stringify(body),
      })
      text = await response.text()
      if (response.status !== 429 || attempt >= 2) break
      console.log(`  … 429 (rate limit) — 62 sn beklenip yeniden denenecek: ${method} ${path}`)
      await new Promise((resolve) => setTimeout(resolve, 62_000))
    }
  } catch (error) {
    const row = { method, path, status: 0, ok: false, label, error: String(error) }
    calls.push(row)
    failed += 1
    console.log(`  ✗ ---  ${method} ${path}  ${label}  → ${error}`)
    return { ok: false, status: 0, data: null, text: String(error) }
  }

  let data = null
  if (text) {
    try {
      data = JSON.parse(text)
    } catch {
      data = null
    }
  }

  // `expected`: sistemin DOĞRU davranışı olan reddetmeler (mükerrer kayıt gibi).
  // Bunlar ikinci koşumun normal hâli; kırmızı yazmak script'i yalancı yapar.
  const tolerated = soft || (data?.code && expected.includes(data.code))
  const ok = response.ok
  calls.push({ method, path, status: response.status, ok: ok || tolerated, label })
  if (!ok && !tolerated) failed += 1

  const mark = ok ? '✓' : tolerated ? '·' : '✗'
  const detail = ok ? label : `${label} ${text.slice(0, 200)}`
  console.log(`  ${mark} ${String(response.status).padEnd(3)} ${method} ${path}  ${detail}`.trimEnd())

  return { ok, status: response.status, data, text }
}

function section(title) {
  console.log(`\n─── ${title}`)
}

function die(message) {
  console.error(`\nDURDU: ${message}`)
  process.exit(1)
}

// Auth uçlarında IP başına dakikada 10 istek sınırı var. Sınıra dayanırsak
// beklemek, testi 429 yüzünden kırmızı göstermekten iyidir.
let authCalls = 0
async function authCall(path, body, options = {}) {
  if (authCalls > 0 && authCalls % 8 === 0) {
    console.log('  … auth rate limit siperi: 62 sn bekleniyor')
    await new Promise((resolve) => setTimeout(resolve, 62_000))
  }
  authCalls += 1
  return call('POST', path, { body, ...options })
}

// ---------------------------------------------------------------------- akış

async function main() {
  console.log(`PsikoAl demo seed — ${API}`)

  const health = await call('GET', '/health')
  if (!health.ok) die('API ayakta değil. Önce: bash scripts/e2e-stack.sh')

  // --- 1. Admin girişi -----------------------------------------------------
  section('1. Admin girişi')
  const adminLogin = await authCall('/auth/login', ADMIN)
  if (!adminLogin.ok) die('Admin girişi başarısız.')
  const adminToken = adminLogin.data.tokens.accessToken

  // --- 2. Kategoriler ------------------------------------------------------
  // Kanonik 17 zaten migration seed'inden geliyor; burada üretilmez, DOĞRULANIR.
  // Kanonik olmayan her satır (E2E artığı, bayat mock-db kopyası) pasifleştirilir
  // — silme ucu yok ve zaten silmek yanlış olurdu: pasifleştirme audit'e düşer.
  section('2. Kategoriler doğrulanıyor + artıklar pasifleştiriliyor')
  const existing = await call('GET', '/admin/categories', { token: adminToken })
  const rows = Array.isArray(existing.data) ? existing.data : (existing.data?.data ?? [])
  const bySlug = new Map(rows.map((c) => [c.slug, c]))

  const missing = CANONICAL_CATEGORIES.filter((slug) => !bySlug.has(slug))
  if (missing.length > 0) {
    console.log(`  ✗ --- migration seed'i eksik: ${missing.join(', ')}`)
    failed += 1
  } else {
    console.log(`  ✓ --- kanonik 17 kategorinin hepsi yerinde`)
  }

  for (const slug of CANONICAL_CATEGORIES) {
    const category = bySlug.get(slug)
    if (category && category.isActive === false) {
      await call('PATCH', `/admin/categories/${category.id}`, {
        token: adminToken,
        label: `${slug} yeniden aktifleştirildi`,
        body: { isActive: true },
      })
    }
  }

  const canonical = new Set(CANONICAL_CATEGORIES)
  const strays = rows.filter((c) => !canonical.has(c.slug) && c.isActive !== false)
  for (const stray of strays) {
    await call('PATCH', `/admin/categories/${stray.id}`, {
      token: adminToken,
      label: `artık pasifleştirildi: ${stray.slug}`,
      body: { isActive: false },
    })
  }
  if (strays.length === 0) console.log('  · --- pasifleştirilecek artık kategori yok')

  // --- 3. Danışan profili --------------------------------------------------
  section('3. Danışan kaydı/girişi + profil')
  const clientRegister = await authCall(
    '/auth/register',
    {
      email: CLIENT.email,
      password: CLIENT.password,
      firstName: CLIENT.firstName,
      lastName: CLIENT.lastName,
      role: 'client',
      termsAccepted: true,
      healthDataConsent: true,
    },
    { soft: true, label: CLIENT.email }
  )

  const clientLogin = clientRegister.ok
    ? clientRegister
    : await authCall('/auth/login', { email: CLIENT.email, password: CLIENT.password })
  if (!clientLogin.ok) die('Danışan ne kaydolabildi ne giriş yapabildi.')
  const clientToken = clientLogin.data.tokens.accessToken
  const clientId = clientLogin.data.user.id

  await call('PATCH', '/auth/me', {
    token: clientToken,
    label: `${CLIENT.firstName} ${CLIENT.lastName}`,
    body: {
      firstName: CLIENT.firstName,
      lastName: CLIENT.lastName,
      phone: '+905551112233',
      city: 'İstanbul',
      shareEmail: false,
      sharePhone: false,
      shareLocation: true,
    },
  })

  // --- 4. Uzmanlar ---------------------------------------------------------
  section('4. Uzman kaydı → profil → admin onayı')
  const experts = []
  for (const expert of EXPERTS) {
    let token
    // Hesap zaten varsa 422 AUTH_EMAIL_ALREADY_REGISTERED gelir; bu ikinci
    // koşumun normal hâli, rapora başarısızlık olarak yazılmaz.
    const register = await authCall(
      '/auth/register',
      {
        email: expert.email,
        password: expert.password,
        firstName: expert.firstName,
        lastName: expert.lastName,
        role: 'expert',
        termsAccepted: true,
        healthDataConsent: true,
      },
      { soft: true, label: expert.email }
    )

    if (register.ok) {
      token = register.data.tokens.accessToken
    } else {
      const login = await authCall('/auth/login', { email: expert.email, password: expert.password })
      if (!login.ok) die(`${expert.email} ne kaydolabildi ne giriş yapabildi.`)
      token = login.data.tokens.accessToken
    }

    let profile = await call('POST', '/experts/profile', {
      token,
      label: `${expert.firstName} ${expert.lastName}`,
      body: expert.profile,
      soft: true,
    })

    if (!profile.ok) {
      // Profil zaten varsa POST reddeder — PATCH ile güncelle.
      profile = await call('PATCH', '/experts/profile', {
        token,
        label: `${expert.firstName} ${expert.lastName} (güncelleme)`,
        body: expert.profile,
      })
      if (!profile.ok) die(`${expert.email} profili oluşturulamadı.`)
    }

    const expertId = profile.data.id
    await call('POST', `/admin/experts/${expertId}/approve`, { token: adminToken, label: 'onay', soft: true })
    await call('POST', `/admin/experts/${expertId}/verify`, {
      token: adminToken,
      label: 'rozet',
      body: { isVerified: true },
    })

    experts.push({ ...expert, token, id: expertId })
  }

  // --- 5. İlanlar ----------------------------------------------------------
  // LISTING_MAX_ACTIVE varsayılanı 3; dördüncü ilan bu sipere takılır. Ayarı
  // admin ucundan gevşetmek hem 4. ilanı mümkün kılar hem system-settings
  // ucunun gerçekten davranışı değiştirdiğini kanıtlar.
  section('5. Sistem ayarı gevşetilir → danışan ilan açar → admin onaylar')
  await call('PATCH', '/admin/system-settings/LISTING_MAX_ACTIVE', {
    token: adminToken,
    label: '3 → 5',
    body: { value: '5' },
  })

  const mine = await call('GET', '/listings/my', { token: clientToken, label: 'mevcut ilanlar' })
  const mineRows = mine.data?.data ?? mine.data ?? []
  const existingTitles = new Set(Array.isArray(mineRows) ? mineRows.map((l) => l.title) : [])

  const listings = []
  for (const listing of LISTINGS) {
    if (existingTitles.has(listing.title)) {
      console.log(`  · --- ilan zaten var: ${listing.title.slice(0, 40)}`)
      const already = mineRows.find((l) => l.title === listing.title)
      listings.push({ ...listing, id: already.id })
      continue
    }
    const created = await call('POST', '/listings', {
      token: clientToken,
      label: listing.title.slice(0, 40),
      body: {
        title: listing.title,
        description: listing.description,
        specialization: listing.specialization,
        budgetMin: listing.budgetMin,
        budgetMax: listing.budgetMax,
        preferredSessionType: listing.preferredSessionType,
        city: listing.city,
        assessmentResultId: null,
      },
    })
    if (!created.ok) continue

    if (listing.approve) {
      await call('POST', `/admin/listings/${created.data.id}/approve`, { token: adminToken, label: 'onay' })
    } else {
      console.log('  · --- bilinçli olarak PENDING_APPROVAL bırakıldı')
    }
    listings.push({ ...listing, id: created.data.id })
  }

  const openListings = listings.filter((l) => l.approve)
  if (openListings.length < 3) die('Yeterli OPEN ilan yok, teklif adımı anlamsız olur.')

  // --- 6. Teklifler --------------------------------------------------------
  section('6. Uzmanlar teklif verir')
  const offers = []
  const offerPlan = [
    { expert: 0, listing: 0, price: 1200, sessionType: 'online', title: 'Haftalık BDT paketi', description: 'İlk seans değerlendirme, sonrasında haftalık 50 dakikalık online seanslar. Ara ödevlerle ilerleriz.' },
    { expert: 1, listing: 0, price: 1350, sessionType: 'online', title: 'Panik atak odaklı program', description: 'Nefes ve maruz bırakma teknikleriyle sekiz haftalık yapılandırılmış program.' },
    { expert: 1, listing: 1, price: 1600, sessionType: 'yüz_yüze_online', title: 'Tükenmişlik programı', description: 'Kariyer kararları ve tükenmişlik için şema terapi temelli süreç.' },
    { expert: 2, listing: 2, price: 2200, sessionType: 'yüz_yüze', title: 'Çift terapisi süreci', description: 'İki haftada bir 80 dakikalık çift seansı, aralarda iletişim ödevleri.' },
  ]

  for (const plan of offerPlan) {
    const expert = experts[plan.expert]
    const listing = openListings[plan.listing]
    if (!expert || !listing) continue

    const offer = await call('POST', '/offers', {
      token: expert.token,
      label: `${expert.firstName} → ${listing.title.slice(0, 30)}`,
      expected: ['OFFER_ALREADY_EXISTS', 'OFFER_LISTING_NOT_OPEN'],
      body: {
        listingId: listing.id,
        title: plan.title,
        price: plan.price,
        sessionType: plan.sessionType,
        description: plan.description,
      },
    })
    if (offer.ok) offers.push({ ...plan, id: offer.data.id, expert, listing })
  }

  // --- 7. Kabul → eşleşme --------------------------------------------------
  section('7. Danışan teklifi kabul eder → eşleşme')
  // Önceki koşum zinciri tamamladıysa yeni eşleşme kurulmaz; mevcut olan bulunur.
  const priorMatches = await call('GET', '/matches', { token: clientToken, label: 'mevcut eşleşmeler' })
  const priorRows = priorMatches.data?.data ?? priorMatches.data ?? []
  const alreadyMatched = Array.isArray(priorRows) && priorRows.length > 0

  const accepted = offers.find((o) => o.listing.id === openListings[0].id)
  let matchId = alreadyMatched ? priorRows[0].id : null
  if (alreadyMatched) {
    console.log(`  · --- eşleşme zaten var: ${matchId} (${priorRows[0].status})`)
  } else if (!accepted) {
    console.log('  ✗ kabul edilecek teklif bulunamadı')
    failed += 1
  } else {
    const accept = await call('POST', `/offers/${accepted.id}/accept`, {
      token: clientToken,
      label: `${accepted.expert.firstName} kabul edildi`,
    })
    if (accept.ok) matchId = accept.data.matchId ?? accept.data.id ?? null
  }

  // --- 8. Karşılıklı bırakma → yorum → moderasyon --------------------------
  section('8. Eşleşmeyi bırakma → yorum → admin onayı')
  if (!matchId) {
    console.log('  ✗ eşleşme yok, yorum zinciri atlandı')
    failed += 1
  } else {
    const releaseExpert = experts.find((e) => e.id === accepted?.expert.id) ?? experts[0]
    await call('POST', `/match/${matchId}/release`, {
      token: clientToken,
      label: 'danışan bıraktı',
      expected: ['MATCH_NOT_ACTIVE'],
      body: { matchId, reason: 'Süreci tamamladık.' },
    })
    await call('POST', `/match/${matchId}/release`, {
      token: releaseExpert.token,
      label: 'uzman bıraktı',
      expected: ['MATCH_NOT_ACTIVE'],
      body: { matchId, reason: 'Hedeflere ulaşıldı.' },
    })

    const review = await call('POST', `/experts/${releaseExpert.id}/reviews`, {
      token: clientToken,
      label: '5 yıldız',
      expected: ['REVIEW_ALREADY_EXISTS'],
      body: {
        matchId,
        rating: 5,
        comment:
          'İlk seanstan itibaren ne yaşadığımı anladığını hissettirdi. Verdiği ödevler günlük hayatta gerçekten işe yaradı, atakların sıklığı belirgin şekilde azaldı.',
        sessionType: 'online',
      },
    })

    if (review.ok) {
      const pending = await call('GET', '/admin/reviews?status=pending', { token: adminToken })
      const rows = pending.data?.data ?? pending.data ?? []
      const target = Array.isArray(rows) ? rows.find((r) => r.id === review.data.id) ?? rows[0] : null
      if (target) {
        await call('POST', `/admin/reviews/${target.id}/approve`, { token: adminToken, label: 'yorum onayı' })
      } else {
        console.log('  ✗ onay kuyruğunda yorum görünmedi')
        failed += 1
      }
    }
  }

  // --- 9. Bildirimler ------------------------------------------------------
  section('9. Admin bildirim gönderir')
  await call('POST', '/admin/notifications/send', {
    token: adminToken,
    label: 'tek kullanıcı',
    body: {
      targetType: 'user',
      userId: clientId,
      role: null,
      city: null,
      specialization: null,
      title: 'İlanınız yayında',
      body: 'Panik atak ilanınız onaylandı ve uzmanlara açıldı. Gelen teklifleri Teklifler sekmesinden görebilirsiniz.',
    },
  })
  await call('POST', '/admin/notifications/send', {
    token: adminToken,
    label: 'danışan segmenti',
    body: {
      targetType: 'segment',
      userId: null,
      role: 'client',
      city: null,
      specialization: null,
      title: 'Yeni testler eklendi',
      body: 'Kaygı ve depresyon tarama testleri güncellendi. Testler sekmesinden ücretsiz çözebilirsiniz.',
    },
  })

  // --- 10. Mobilin gördüğü uçlar ------------------------------------------
  section('10. Mobil uçları doğrula (danışan gözüyle)')
  await call('GET', '/categories', { token: clientToken, label: 'keşfet' })
  await call('GET', '/listings/my', { token: clientToken, label: 'ilanlarım' })
  await call('GET', '/offers/my', { token: clientToken, label: 'tekliflerim' })
  await call('GET', '/matches', { token: clientToken, label: 'eşleşmelerim' })
  await call('GET', '/notifications', { token: clientToken, label: 'bildirimler' })
  await call('GET', '/assessment', { token: clientToken, label: 'testler' })
  await call('GET', '/assessment/results/my', { token: clientToken, label: 'test sonuçlarım' })
  await call('GET', '/auth/me', { token: clientToken, label: 'profilim' })
  if (experts[0]) {
    await call('GET', `/experts/${experts[0].id}`, { token: clientToken, label: 'uzman detayı' })
    await call('GET', `/experts/${experts[0].id}/reviews`, { token: clientToken, label: 'uzman yorumları' })
  }
  await call('GET', '/categories/anksiyete', { token: clientToken, label: 'kategori detayı' })

  section('11. Mobil uçları doğrula (uzman gözüyle)')
  if (experts[0]) {
    await call('GET', '/listings', { token: experts[0].token, label: 'ilan akışı' })
    await call('GET', '/offers/my', { token: experts[0].token, label: 'verdiğim teklifler' })
    await call('GET', '/matches', { token: experts[0].token, label: 'eşleşmelerim' })
  }

  // --- Özet ----------------------------------------------------------------
  const total = calls.length
  const bad = calls.filter((c) => !c.ok)
  console.log(`\n${'═'.repeat(70)}`)
  console.log(`Toplam çağrı: ${total}   Başarısız: ${bad.length}`)
  if (bad.length > 0) {
    console.log('\nBaşarısız çağrılar:')
    for (const c of bad) console.log(`  ${String(c.status).padEnd(3)} ${c.method} ${c.path}  ${c.label}`)
  }
  process.exit(failed > 0 ? 1 : 0)
}

main().catch((error) => {
  console.error(error)
  process.exit(1)
})
