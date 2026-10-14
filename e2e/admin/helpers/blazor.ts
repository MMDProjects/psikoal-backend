import { expect, type Page } from '@playwright/test'

/**
 * Blazor Server sayfasına gider ve circuit kurulana kadar bekler.
 *
 * Neden gerekli: prerender kapalı (App.razor) — sayfa içeriği ancak SignalR
 * circuit'i kurulunca çizilir. Circuit hazır olmadan yapılan `fill()` C# alanına
 * HİÇ ulaşmaz — form boş gönderilir ve test "sebepsiz" kırmızı olur.
 *
 * Beklenen sinyal SignalR WebSocket'i: Blazor Server circuit'i onun üzerinden
 * kurar, yani açılmış bir WebSocket = interaktif render başlamış demektir.
 * `window.Blazor` kontrolü ikinci sipertir (script yüklendi mi).
 */
export async function gotoInteractive(page: Page, path: string): Promise<void> {
  const socket = page.waitForEvent('websocket', { timeout: 30_000 })
  await page.goto(path)
  await socket
  await page.waitForFunction(() => 'Blazor' in window, null, { timeout: 30_000 })
}

/**
 * Admin paneline gerçek kimlikle giriş yapar.
 *
 * Oturum sekmenin sessionStorage'ında (ProtectedSessionStorage, Data Protection
 * ile şifreli) tutulur: aynı sekmede yenileme oturumu korur, ama `storageState`
 * yalnız cookie + localStorage taşıdığı için oturum yeni bir bağlama AKTARILAMAZ.
 * Bu yüzden her test bir kez login olur — ve tam bu yüzden worker sayısı 1
 * (rate limit).
 */
export async function loginAsAdmin(page: Page): Promise<void> {
  const email = requireEnv('E2E_ADMIN_EMAIL')
  const password = requireEnv('E2E_ADMIN_PASSWORD')

  await gotoInteractive(page, '/login')

  await page.getByTestId('login-email').fill(email)
  await page.getByTestId('login-password').fill(password)
  await page.getByTestId('login-submit').click()

  // Giriş başarılıysa "/" ye yönlenir. Hata mesajı görünürse onu göster —
  // "timeout" yerine gerçek sebebi okumak hata ayıklamayı kısaltır.
  await expect
    .poll(async () => {
      if (await page.getByTestId('login-error').isVisible().catch(() => false)) {
        return `LOGIN_ERROR: ${await page.getByTestId('login-error').innerText()}`
      }
      return new URL(page.url()).pathname
    }, { timeout: 30_000 })
    .toBe('/')
}

/**
 * Panel içi sayfa geçişi — nav bağlantısına tıklayarak.
 *
 * `page.goto` artık oturumu düşürmez (MainLayout oturumu sessionStorage'dan geri
 * yükleyip /admin/me ile doğrular), ama her seferinde yeni circuit demektir. Nav
 * bağlantısı SPA geçişi yapar: daha hızlı, ve menünün role göre süzüldüğünü de
 * dolaylı olarak doğrular (super_admin'e her bağlantı görünmeli).
 */
export async function navigateTo(page: Page, label: string): Promise<void> {
  await page.getByRole('link', { name: label, exact: true }).click()
  await expect
    .poll(() => new URL(page.url()).pathname, { timeout: 20_000 })
    .not.toBe('/login')
}

export function requireEnv(name: string): string {
  const value = process.env[name]
  if (!value) {
    throw new Error(
      `E2E ortam değişkeni tanımsız: ${name}\n` +
        `Stack'i kaldırıp ortamı besleyin:  source scripts/e2e-stack.sh`
    )
  }
  return value
}
