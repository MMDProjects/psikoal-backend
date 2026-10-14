import { expect, test } from '@playwright/test'

import { gotoInteractive, loginAsAdmin, requireEnv } from '../helpers/blazor'

/**
 * Admin panelin kendi duman testi.
 *
 * Bu dosya kırmızıysa kategori zinciri testine bakmaya gerek yok: panel ya
 * ayakta değil ya da API ile konuşamıyor.
 */
test.describe('Admin paneli', () => {
  test('giriş yapılmadan panel açılmaz', async ({ page }) => {
    await gotoInteractive(page, '/categories')

    // MainLayout oturumu bulamayınca @Body'yi hiç çizmeden /login'e yönlendiriyor;
    // dönüş adresi returnUrl'de taşınıyor.
    await expect.poll(() => new URL(page.url()).pathname, { timeout: 20_000 }).toBe('/login')
    expect(new URL(page.url()).searchParams.get('returnUrl')).toBe('/categories')
    await expect(page.getByTestId('category-table')).toHaveCount(0)
  })

  test('yanlış şifre hata mesajı gösterir', async ({ page }) => {
    await gotoInteractive(page, '/login')

    await page.getByTestId('login-email').fill(requireEnv('E2E_ADMIN_EMAIL'))
    await page.getByTestId('login-password').fill('kesinlikle-yanlis-sifre')
    await page.getByTestId('login-submit').click()

    await expect(page.getByTestId('login-error')).toBeVisible({ timeout: 20_000 })
    // Yönlenme OLMAMALI — hata gösterip aynı sayfada kalmalı.
    expect(new URL(page.url()).pathname).toBe('/login')
  })

  test('doğru kimlikle giriş yapılır ve kategori tablosu yüklenir', async ({ page }) => {
    await loginAsAdmin(page)

    await page.getByRole('link', { name: 'Kategoriler' }).click()
    await expect(page.getByTestId('category-table')).toBeVisible({ timeout: 20_000 })

    // Tabloda en az bir satır olmalı: migration 17 kategori seed'liyor.
    // Boş tablo "API'ye bağlanamadım" hatasının sessiz hâlidir.
    await expect(page.getByTestId('category-row-anksiyete')).toBeVisible({ timeout: 20_000 })
  })
})
