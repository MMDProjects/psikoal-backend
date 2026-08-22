import { defineConfig, devices } from '@playwright/test'

const isCI = !!process.env.CI

export default defineConfig({
  testDir: './tests',
  outputDir: './.artifacts/test-results',

  // Paralellik KAPALI ve tek worker. İki bağımsız sebep:
  //  1. Senaryolar aynı Supabase örneğini ve aynı admin kullanıcısını paylaşıyor.
  //  2. API'de "auth" rate limit politikası IP başına dakikada 10 istek
  //     (src/PsikoAl.Api/Program.cs). Paralel worker'lar tek IP'den vurur ve
  //     testler sözleşme değil 429 yüzünden kırmızı olurdu.
  fullyParallel: false,
  workers: 1,

  // Lokalde retry YOK: flaky'i gizlemek, flaky'i kaybetmektir.
  // CI'da 1 retry — runner gürültüsü tek başına merge'ü bloklamasın.
  retries: isCI ? 1 : 0,

  // CI'da .only ile daraltılmış bir suite yanlışlıkla merge edilmesin.
  forbidOnly: isCI,

  reporter: isCI
    ? [['list'], ['html', { outputFolder: './.artifacts/report', open: 'never' }]]
    : [['list']],

  use: {
    baseURL: process.env.ADMIN_URL ?? 'http://localhost:5057',

    // Ağır artifact YALNIZ hata durumunda. Yeşil koşumda depolama maliyeti sıfır.
    trace: 'retain-on-failure',
    screenshot: 'only-on-failure',
    video: 'off',

    // Blazor Server SignalR üzerinden konuşur; yavaş bir circuit kurulumunda
    // varsayılan 5sn yetmiyor.
    actionTimeout: 15_000,
    navigationTimeout: 30_000,
  },

  projects: [{ name: 'chromium', use: { ...devices['Desktop Chrome'] } }],

  // `webServer` bilinçli olarak YOK. Bu suite hazır bir stack'e karşı koşar:
  // Supabase (54321) + PsikoAl.Api (5089) + PsikoAl.Client (5057). Playwright'ın
  // üçünü de kaldırması, Supabase'in Docker bağımlılığı yüzünden tek süreçle
  // yönetilemez. Stack'i scripts/e2e-stack.sh kaldırır.
})
