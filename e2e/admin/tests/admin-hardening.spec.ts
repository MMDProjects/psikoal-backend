import { expect, test } from '@playwright/test'

import { gotoInteractive, loginAsAdmin, navigateTo, requireEnv } from '../helpers/blazor'

/**
 * Panel sertleştirmesinin uçtan uca kanıtı (fix/ADMIN-1-panel-sertlestirme).
 *
 * Login sayısı bilinçli olarak az: API'nin "auth" politikası IP başına dakikada
 * 10 istek. Oturum gerektiren kontroller tek testte, tek girişle yapılıyor.
 */
test.describe('Admin paneli sertleştirme', () => {
  test('yanıtlar güvenlik başlıklarını taşır', async ({ request }) => {
    const response = await request.get('/login')

    const headers = response.headers()
    expect(headers['content-security-policy']).toContain("frame-ancestors 'none'")
    expect(headers['content-security-policy']).toContain("script-src 'self'")
    expect(headers['x-frame-options']).toBe('DENY')
    expect(headers['x-content-type-options']).toBe('nosniff')
    expect(headers['referrer-policy']).toBe('no-referrer')

    const html = await response.text()
    expect(html).toContain('<html lang="tr">')
  })

  test('admin olmayan hesap panele giremez', async ({ page }) => {
    // Mobil danışan hesabı GoTrue'da var ama admin_users satırı YOK
    // (SupabaseTestIdentity.EnsureClientAsync). Supabase girişi başarılı olur,
    // /admin/me 403 döner ve panel girişi reddetmeli.
    await gotoInteractive(page, '/login')
    await page.getByTestId('login-email').fill(requireEnv('E2E_CLIENT_EMAIL'))
    await page.getByTestId('login-password').fill(requireEnv('E2E_CLIENT_PASSWORD'))
    await page.getByTestId('login-submit').click()

    await expect(page.getByTestId('login-error')).toContainText('yönetici yetkisi yok', { timeout: 20_000 })
    expect(new URL(page.url()).pathname).toBe('/login')
  })

  test('oturum yenilemede korunur, onay diyalogları işlem yapmadan kapanır, çıkış oturumu siler', async ({ page }) => {
    await loginAsAdmin(page)

    // 1) Sayfa yenilemesi oturumu düşürmez (ProtectedSessionStorage).
    const socket = page.waitForEvent('websocket', { timeout: 30_000 })
    await page.reload()
    await socket
    await expect(page.getByRole('heading', { name: 'Genel Bakış' }).first()).toBeVisible({ timeout: 20_000 })
    expect(new URL(page.url()).pathname).toBe('/')

    // 2) Bildirim gönderimi önce alıcı sayısını onaylatır. "Vazgeç" hiçbir şey göndermez.
    await navigateTo(page, 'Bildirimler')
    await page.getByRole('tab', { name: 'Manuel Gönder' }).click()
    await page.getByTestId('notification-title').fill('E2E duyuru — gönderilmeyecek')
    await page.getByTestId('notification-body').locator('textarea').fill('Onay diyaloğunda vazgeçilecek.')
    await page.getByTestId('notification-send').click()

    const confirm = page.getByRole('alertdialog')
    await expect(confirm).toBeVisible({ timeout: 20_000 })
    await expect(confirm).toContainText('aktif kullanıcıya gönderilecek')
    await confirm.getByRole('button', { name: 'Vazgeç' }).click()
    await expect(confirm).toBeHidden()

    await page.getByRole('tab', { name: 'Gönderim Geçmişi' }).click()
    await expect(page.getByText('E2E duyuru — gönderilmeyecek')).toHaveCount(0)

    // 3) Yayındaki bir ilanı kapatmak onay ister; "Vazgeç" ilanı açık bırakır.
    await navigateTo(page, 'İlanlar')
    await page.locator('select').first().selectOption('OPEN')
    await page.getByRole('button', { name: 'Filtrele' }).click()
    const reviewButton = page.getByRole('button', { name: 'İncele' }).first()
    if (await reviewButton.isVisible({ timeout: 10_000 }).catch(() => false)) {
      await reviewButton.click()
      await page.getByTestId('listing-close').click()
      const closeDialog = page.getByRole('alertdialog')
      await expect(closeDialog).toContainText('İlanı kapat')
      await closeDialog.getByRole('button', { name: 'Vazgeç' }).click()
      await expect(closeDialog).toBeHidden()
      await expect(page.getByTestId('listing-close')).toBeVisible()
    }

    // 4) Denetim kaydı sayfası açılır ve sayfalı tablo ya da boş durum gösterir.
    await navigateTo(page, 'Denetim Kaydı')
    // Login ve açılış zaten audit üretmez; ama önceki testler (kategori zinciri)
    // kayıt bırakmış olabilir — iki durum da geçerli, hata mesajı geçerli değil.
    const auditCard = page.getByTestId('audit-card')
    await expect(auditCard.getByTestId('pager').or(auditCard.getByText('Kayıt bulunamadı'))).toBeVisible({
      timeout: 20_000,
    })

    // 5) Çıkış oturumu siler: yenilemeden sonra panel tekrar giriş ister.
    await page.getByTestId('logout').click()
    await expect.poll(() => new URL(page.url()).pathname, { timeout: 20_000 }).toBe('/login')
    await gotoInteractive(page, '/')
    await expect.poll(() => new URL(page.url()).pathname, { timeout: 20_000 }).toBe('/login')
  })
})
