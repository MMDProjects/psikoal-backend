#!/usr/bin/env bash
# E2E koşumlarının bıraktığı test verisini siler.
#
# NİYE GEREKLİ: kategori silme ucu yok (admin panelde yalnız pasifleştirme var),
# bu yüzden Playwright akışları oluşturdukları satırı geri alamıyor. CI'da sorun
# değil — her koşum temiz bir stack'te başlar. Lokalde satırlar birikir ve
# admin panelin kategori tablosunu okunmaz hâle getirir.
#
# .NET E2E testleri KENDİ ürettiklerini zaten temizliyor (E2EFixture.DisposeAsync);
# bu script Playwright'ın bıraktıklarını hedefler.
set -euo pipefail
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

# `supabase db psql` DEĞİL: o alt komut CLI 2.x'te yok. Doğrusu `db query --local`.
npx --yes supabase db query --local "delete from public.categories where slug like 'e2e-%';"
echo "e2e-* kategorileri silindi."
