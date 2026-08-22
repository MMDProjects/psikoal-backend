import { mkdirSync, writeFileSync } from 'node:fs'
import { dirname, resolve } from 'node:path'
import { fileURLToPath } from 'node:url'

import { expect, test } from '@playwright/test'

import { loginAsAdmin, navigateTo } from '../helpers/blazor'

const here = dirname(fileURLToPath(import.meta.url))

/**
 * Slug'ın Maestro'ya devri.
 *
 * Bir Playwright testi emülatör süremez, bir Maestro akışı da tarayıcı süremez.
 * Zinciri birleştiren şey ortak slug: burada üretilir, dosyaya yazılır, mobil
 * adım onu `E2E_EXPECTED_CATEGORY_SLUG` olarak okur (scripts/e2e-mobile.sh).
 *
 * Ortamdan geliyorsa ONA saygı duyulur — CI tek slug'ı iki adıma da verir ve
 * dosya sistemine hiç bağımlı kalmaz.
 */
const SLUG_FILE = resolve(here, '../.artifacts/category-slug.txt')

function resolveSlug(): string {
  const fromEnv = process.env.E2E_CATEGORY_SLUG
  if (fromEnv) {
    return fromEnv
  }
  // Slug regex'i backend'de `^[a-z0-9-]+$` (CreateCategoryDtoValidator).
  return `e2e-${Math.random().toString(36).slice(2, 10)}`
}

test.describe('Admin -> mobil kategori zinciri', () => {
  test('panelden eklenen kategori listeye düşer ve mobil uca yayılır', async ({ page, request }) => {
    const slug = resolveSlug()

    await loginAsAdmin(page)
    await navigateTo(page, 'Kategoriler')
    await expect(page.getByTestId('category-table')).toBeVisible({ timeout: 20_000 })

    // Eklemeden ÖNCE yok olduğunu doğrula. Bu satır olmadan test, kategori
    // zaten var olduğu için de yeşil olurdu — yani hiçbir şey kanıtlamazdı.
    await expect(page.getByTestId(`category-row-${slug}`)).toHaveCount(0)

    await page.getByTestId('category-new-slug').fill(slug)
    await page.getByTestId('category-new-name').fill(`E2E ${slug}`)
    await page.getByTestId('category-new-icon').fill('brain')
    await page.getByTestId('category-new-blogtag').fill(slug)
    await page.getByTestId('category-new-summary').fill('E2E akışının ürettiği kategori.')
    await page.getByTestId('category-new-description').fill('Otomatik test verisi.')

    await page.getByTestId('category-create-submit').click()

    // 1. halka: admin panelin kendi tablosunda göründü.
    await expect(page.getByTestId(`category-row-${slug}`)).toBeVisible({ timeout: 30_000 })

    // 2. halka: mobil uygulamanın çağırdığı KAMUYA AÇIK uçta göründü.
    // Panel tablosu /admin/categories'i (tümü), mobil /categories'i (yalnız aktif)
    // okuyor — ikisi farklı sorgu. Bu yüzden ayrıca doğrulanır.
    const apiUrl = process.env.E2E_API_URL ?? 'http://localhost:5089'
    const response = await request.get(`${apiUrl}/categories`)
    expect(response.ok()).toBeTruthy()
    const body = (await response.json()) as { data: Array<{ slug: string }> }
    expect(body.data.map((category) => category.slug)).toContain(slug)

    // 3. halka mobil tarafta: slug'ı Maestro adımına devret.
    mkdirSync(dirname(SLUG_FILE), { recursive: true })
    writeFileSync(SLUG_FILE, slug, 'utf8')
    // eslint-disable-next-line no-console
    console.log(`[zincir] mobil adıma devredilen slug: ${slug}`)
  })

  test('panelden pasifleştirilen kategori mobil uçtan kalkar', async ({ page, request }) => {
    const slug = `e2e-off-${Math.random().toString(36).slice(2, 8)}`
    const apiUrl = process.env.E2E_API_URL ?? 'http://localhost:5089'

    await loginAsAdmin(page)
    await navigateTo(page, 'Kategoriler')
    await expect(page.getByTestId('category-table')).toBeVisible({ timeout: 20_000 })

    await page.getByTestId('category-new-slug').fill(slug)
    await page.getByTestId('category-new-name').fill(`E2E ${slug}`)
    await page.getByTestId('category-new-icon').fill('brain')
    await page.getByTestId('category-new-blogtag').fill(slug)
    await page.getByTestId('category-new-summary').fill('Pasifleştirme senaryosu.')
    await page.getByTestId('category-new-description').fill('Otomatik test verisi.')
    await page.getByTestId('category-create-submit').click()

    await expect(page.getByTestId(`category-row-${slug}`)).toBeVisible({ timeout: 30_000 })

    await page.getByTestId(`category-toggle-${slug}`).click()

    // Buton etiketi "Pasifleştir" -> "Aktifleştir" olmalı: durum gerçekten değişti.
    await expect(page.getByTestId(`category-toggle-${slug}`)).toHaveText(/Aktifleştir/, {
      timeout: 30_000,
    })

    const response = await request.get(`${apiUrl}/categories`)
    const body = (await response.json()) as { data: Array<{ slug: string }> }
    expect(body.data.map((category) => category.slug)).not.toContain(slug)
  })
})
